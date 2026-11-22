using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace B2BStore.Settings.Api;

public static class RollbackWriteTestGuard
{
    public const string TargetDatabase = "Store5_Settings_Test";
    public const string TargetMemberCode = "store5";
    public const string TargetDataSource = @"(localdb)\MSSQLLocalDB";

    public static void EnsureAllowed(bool testModeEnabled, bool authenticated, string? memberCode, string? connectionString)
    {
        if (!authenticated)
            throw new SettingsException("TestWriteUnauthorized", "An authenticated local administrator is required for the rollback test.");
        if (!testModeEnabled)
            throw new SettingsException("TestWriteModeRequired", "The rollback-only SQL test path is disabled.");
        if (!string.Equals(memberCode, TargetMemberCode, StringComparison.Ordinal))
            throw new SettingsException("DatabaseIsolationViolation", "The rollback test is restricted to the configured store5 record.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new SettingsException("DatabaseNotConfigured", "The local test database connection is not configured.");

        SqlConnectionStringBuilder builder;
        try { builder = new SqlConnectionStringBuilder(connectionString); }
        catch (ArgumentException) { throw new SettingsException("DatabaseIsolationViolation", "The rollback test connection is not an approved local test database."); }

        var isApprovedTarget = string.Equals(builder.InitialCatalog, TargetDatabase, StringComparison.Ordinal)
            && string.Equals(builder.DataSource.Trim(), TargetDataSource, StringComparison.OrdinalIgnoreCase)
            && builder.IntegratedSecurity
            && string.IsNullOrEmpty(builder.UserID)
            && string.IsNullOrEmpty(builder.Password)
            && string.IsNullOrEmpty(builder.AttachDBFilename);
        if (!isApprovedTarget)
            throw new SettingsException("DatabaseIsolationViolation", "The rollback test is restricted to Store5_Settings_Test on the approved LocalDB instance.");
    }
}

public sealed class SqlRollbackOnlySettingsTestService(
    ParameterCatalog catalog,
    string? connectionString,
    string? memberCode,
    bool testModeEnabled)
{
    private const string SiteStatusParameterId = "ctl00_PageContent_ddlSiteStatus";
    private const string ParameterOrderParameterId = "ctl00_PageContent_ddlOrderParameter";
    private const string SiteStatusColumn = "SiteStatus";
    private const string ParameterOrderColumn = "ParameterOrder";
    private static readonly IReadOnlySet<string> AllowedColumns = new HashSet<string>(StringComparer.Ordinal)
    {
        SiteStatusColumn,
        ParameterOrderColumn,
    };

    public async Task<RollbackOnlySettingsTestResponse> RunAsync(
        RollbackOnlySettingsTestRequest request,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        RollbackWriteTestGuard.EnsureAllowed(testModeEnabled, authenticated, memberCode, connectionString);
        ValidateRequest(request);

        var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(cancellationToken); }
        catch (SqlException)
        {
            await connection.DisposeAsync();
            throw new SettingsException("DatabaseNotConfigured", "The approved local test database could not be reached.");
        }

        await using (connection)
        {
            await EnsureConnectedDatabaseAsync(connection, cancellationToken);
            await EnsureExpectedSchemaAsync(connection, cancellationToken);

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            SettingsSnapshot before;
            SettingsSnapshot during;
            try
            {
                before = await ReadTargetAsync(connection, transaction, cancellationToken);
                var expectedOrderDuring = before.ParameterOrder;

                if (request.SiteStatus is { } siteStatus)
                    await UpdateSiteStatusAsync(connection, transaction, siteStatus, cancellationToken);

                if (request.ParameterOrder is { } parameterOrder)
                {
                    var expectedCurrent = ParseNullableBigint(before.ParameterOrder);
                    var nextValue = BitmaskEditor.Apply(expectedCurrent, parameterOrder.Operations, AllowedOptionValues());
                    expectedOrderDuring = nextValue.ToString(CultureInfo.InvariantCulture);
                    await UpdateParameterOrderAsync(connection, transaction, parameterOrder.ExpectedValue, expectedOrderDuring, cancellationToken);
                }

                during = await ReadTargetAsync(connection, transaction, cancellationToken);
                VerifyReadAfterWrite(request, during, expectedOrderDuring);
            }
            finally
            {
                // This path deliberately has no commit operation. Rollback ignores a cancelled request token.
                await transaction.RollbackAsync(CancellationToken.None);
            }

            var afterRollback = await ReadTargetAsync(connection, null, cancellationToken);
            if (afterRollback != before)
                throw new SettingsException("RollbackVerificationFailed", "The rollback verification did not match the original test row values.");

            return new RollbackOnlySettingsTestResponse(
                before.SiteStatus,
                during.SiteStatus,
                afterRollback.SiteStatus,
                before.ParameterOrder,
                during.ParameterOrder,
                afterRollback.ParameterOrder,
                RolledBack: true);
        }
    }

    private void ValidateRequest(RollbackOnlySettingsTestRequest request)
    {
        if (request is null || (request.SiteStatus is null && request.ParameterOrder is null))
            throw new SettingsException("ValidationFailed", "At least one allowlisted setting test change is required.");

        var siteDefinition = FindAllowlistedDefinition(SiteStatusParameterId, SiteStatusColumn);
        var active = siteDefinition.Options.Any(option => option.Value == "1" && option.Label == "Active");
        var inactive = siteDefinition.Options.Any(option => option.Value == "0" && option.Label == "Inactive");
        if (!active || !inactive)
            throw new SettingsException("ValidationFailed", "The site status options do not match the approved test mapping.");
        if (request.SiteStatus is { NewValue: not (0 or 1) })
            throw new SettingsException("ValidationFailed", "SiteStatus test values must be Active (1) or Inactive (0).");

        if (request.ParameterOrder is { } order)
        {
            var definition = FindAllowlistedDefinition(ParameterOrderParameterId, ParameterOrderColumn);
            if (order.Operations is null || order.Operations.Count == 0)
                throw new SettingsException("ValidationFailed", "ParameterOrder tests require at least one listed option operation.");
            _ = ParseNullableBigint(order.ExpectedValue);
            _ = BitmaskEditor.Apply(0, order.Operations, AllowedOptionValues(definition));
        }

        if (!AllowedColumns.SetEquals([SiteStatusColumn, ParameterOrderColumn]))
            throw new SettingsException("ValidationFailed", "The rollback test field allowlist is invalid.");
    }

    private ParameterDefinition FindAllowlistedDefinition(string parameterId, string columnName)
    {
        var definition = catalog.AllSettings.SingleOrDefault(setting => setting.ParameterId == parameterId);
        if (definition is null
            || definition.Mapping.TableCandidate != "DealerSettings"
            || definition.Mapping.ColumnCandidate != columnName)
            throw new SettingsException("ValidationFailed", "An allowlisted setting no longer matches the approved catalog definition.");
        return definition;
    }

    private IReadOnlySet<string> AllowedOptionValues() => AllowedOptionValues(FindAllowlistedDefinition(ParameterOrderParameterId, ParameterOrderColumn));

    private static IReadOnlySet<string> AllowedOptionValues(ParameterDefinition definition)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in definition.Options)
        {
            if (!long.TryParse(option.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var mask)
                || mask <= 0
                || (mask & (mask - 1)) != 0)
                throw new SettingsException("ValidationFailed", "The ParameterOrder catalog contains an option that is not a positive power of two.");
            values.Add(option.Value);
        }
        return values;
    }

    private static long? ParseNullableBigint(string? value)
    {
        if (value is null) return null;
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        throw new SettingsException("ValidationFailed", "ParameterOrder values must be SQL bigint decimal strings.");
    }

    private static async Task EnsureConnectedDatabaseAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT DB_NAME();", connection);
        var databaseName = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (!string.Equals(databaseName, RollbackWriteTestGuard.TargetDatabase, StringComparison.Ordinal))
            throw new SettingsException("DatabaseIsolationViolation", "The connected database is not the approved isolated test database.");
    }

    private static async Task EnsureExpectedSchemaAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT c.name, t.name FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID(N'dbo.DealerSettings') AND c.name IN (N'SiteStatus', N'ParameterOrder');";
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var types = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken)) types[reader.GetString(0)] = reader.GetString(1);
        if (!types.TryGetValue(SiteStatusColumn, out var siteType) || !string.Equals(siteType, "int", StringComparison.OrdinalIgnoreCase)
            || !types.TryGetValue(ParameterOrderColumn, out var orderType) || !string.Equals(orderType, "bigint", StringComparison.OrdinalIgnoreCase))
            throw new SettingsException("DatabaseIsolationViolation", "The isolated test database schema does not match the approved settings types.");
    }

    private static async Task<SettingsSnapshot> ReadTargetAsync(SqlConnection connection, SqlTransaction? transaction, CancellationToken cancellationToken)
    {
        await using (var countCommand = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.DealerSettings WITH (UPDLOCK, HOLDLOCK) WHERE MemberCode = @memberCode;", connection, transaction))
        {
            countCommand.Parameters.AddWithValue("@memberCode", RollbackWriteTestGuard.TargetMemberCode);
            var rowCount = Convert.ToInt64(await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
            RowSelectionGuard.EnsureSingleTarget(rowCount);
        }

        await using var command = new SqlCommand("SELECT [SiteStatus], [ParameterOrder] FROM dbo.DealerSettings WITH (UPDLOCK, HOLDLOCK) WHERE MemberCode = @memberCode;", connection, transaction);
        command.Parameters.AddWithValue("@memberCode", RollbackWriteTestGuard.TargetMemberCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new SettingsException("MemberCodeMissing", "The configured store5 settings row is not available.");
        int? siteStatus = reader.IsDBNull(0) ? null : Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
        string? parameterOrder = reader.IsDBNull(1) ? null : Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        return new SettingsSnapshot(siteStatus, parameterOrder);
    }

    private static async Task UpdateSiteStatusAsync(SqlConnection connection, SqlTransaction transaction, SiteStatusRollbackChange change, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE dbo.DealerSettings SET [SiteStatus] = @newValue WHERE MemberCode = @memberCode AND (([SiteStatus] = @expectedValue) OR ([SiteStatus] IS NULL AND @expectedValue IS NULL));";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", "int", change.NewValue));
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", "int", change.ExpectedValue));
        command.Parameters.AddWithValue("@memberCode", RollbackWriteTestGuard.TargetMemberCode);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new SettingsException("ConcurrencyConflict", "SiteStatus changed before the rollback test could update it.");
    }

    private static async Task UpdateParameterOrderAsync(SqlConnection connection, SqlTransaction transaction, string? expectedValue, string newValue, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE dbo.DealerSettings SET [ParameterOrder] = @newValue WHERE MemberCode = @memberCode AND (([ParameterOrder] = @expectedValue) OR ([ParameterOrder] IS NULL AND @expectedValue IS NULL));";
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", "bigint", newValue));
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", "bigint", expectedValue));
        command.Parameters.AddWithValue("@memberCode", RollbackWriteTestGuard.TargetMemberCode);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new SettingsException("ConcurrencyConflict", "ParameterOrder changed before the rollback test could update it.");
    }

    private static void VerifyReadAfterWrite(RollbackOnlySettingsTestRequest request, SettingsSnapshot during, string? expectedOrderDuring)
    {
        if (request.SiteStatus is { } site && during.SiteStatus != site.NewValue)
            throw new SettingsException("ReadbackMismatch", "SiteStatus did not match the requested value inside the transaction.");
        if (request.ParameterOrder is not null && !string.Equals(during.ParameterOrder, expectedOrderDuring, StringComparison.Ordinal))
            throw new SettingsException("ReadbackMismatch", "ParameterOrder did not match the computed value inside the transaction.");
    }

    private sealed record SettingsSnapshot(int? SiteStatus, string? ParameterOrder);
}
