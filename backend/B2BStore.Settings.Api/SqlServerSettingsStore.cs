using System.Globalization;
using Microsoft.Data.SqlClient;

namespace B2BStore.Settings.Api;

public sealed class SqlServerSettingsStore(ParameterCatalog catalog, string? connectionString, string? memberCode) : ISettingsStore
{
    public async Task<SettingsResponse> ReadAsync(CancellationToken cancellationToken)
    {
        var (connection, columnTypes) = await OpenAndInspectAsync(cancellationToken);
        await using (connection)
        {
            var rowCount = await GetTargetRowCountAsync(connection, null, cancellationToken);
            RowSelectionGuard.EnsureSingleTarget(rowCount);
            var definitions = catalog.AllSettings.Where(item => item.Mapping.TableCandidate == "DealerSettings" && item.Mapping.ColumnCandidate is not null).ToArray();
            var available = definitions.Where(item => columnTypes.ContainsKey(item.Mapping.ColumnCandidate!)).ToArray();
            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            if (available.Length > 0)
            {
                var columns = string.Join(", ", available.Select(item => QuoteIdentifier(item.Mapping.ColumnCandidate!)));
                await using var command = new SqlCommand($"SELECT {columns} FROM dbo.DealerSettings WHERE MemberCode = @memberCode;", connection);
                command.Parameters.AddWithValue("@memberCode", memberCode!);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken)) throw new SettingsException("MemberCodeMissing", "The configured member record is no longer available.");
                for (var index = 0; index < available.Length; index++)
                {
                    var normalized = Normalize(reader.GetValue(index), reader.GetFieldType(index), columnTypes[available[index].Mapping.ColumnCandidate!]);
                    values[available[index].ParameterId] = DealerSettingValueCodec.ToApiValue(available[index].ParameterId, normalized);
                }
            }
            await SqlExtensionSettingsPersistence.ApplyReadValuesAsync(catalog, connection, values, memberCode!, cancellationToken);
            foreach (var item in catalog.AllSettings) values.TryAdd(item.ParameterId, null);
            return BuildResponse(values, "SQL values include candidate field-name matches. Such fields remain read-only until persistence mappings are verified.");
        }
    }

    public Task<SettingsResponse> UpdateAsync(UpdateRequest request, CancellationToken cancellationToken) =>
        throw new SettingsException("ReadOnlyMode", "Settings are read-only; SQL writes are disabled.");

    private async Task<(SqlConnection Connection, Dictionary<string, string> ColumnTypes)> OpenAndInspectAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) throw new SettingsException("DatabaseNotConfigured", "Set B2BSTORE_SQL_CONNECTION to the isolated analysis database connection.");
        if (string.IsNullOrWhiteSpace(memberCode)) throw new SettingsException("MemberCodeMissing", "Set B2BSTORE_MEMBER_CODE to the exact local analysis record context.");
        var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(cancellationToken); }
        catch (SqlException) { await connection.DisposeAsync(); throw new SettingsException("DatabaseNotConfigured", "The configured SQL Server database could not be reached."); }

        await using var command = new SqlCommand("SELECT c.name, t.name FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id WHERE c.object_id = OBJECT_ID(N'dbo.DealerSettings');", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            columns[name] = reader.GetString(1);
        }
        await reader.CloseAsync();
        if (!columns.ContainsKey("MemberCode"))
        {
            await connection.DisposeAsync();
            throw new SettingsException("DatabaseNotConfigured", "dbo.DealerSettings or its MemberCode column is unavailable in the configured database.");
        }
        return (connection, columns);
    }

    private async Task<long> GetTargetRowCountAsync(SqlConnection connection, SqlTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.DealerSettings WHERE MemberCode = @memberCode;", connection, transaction);
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<Dictionary<string, object?>> ReadCurrentValuesAsync(SqlConnection connection, IReadOnlyDictionary<string, string> columnTypes, CancellationToken cancellationToken)
    {
        var available = catalog.AllSettings.Where(item => item.Mapping.Status == "verified" && item.Mapping.ColumnCandidate is not null && columnTypes.ContainsKey(item.Mapping.ColumnCandidate)).ToArray();
        var values = catalog.AllSettings.ToDictionary(item => item.ParameterId, _ => (object?)null, StringComparer.Ordinal);
        if (available.Length == 0) return values;
        var columns = string.Join(", ", available.Select(item => QuoteIdentifier(item.Mapping.ColumnCandidate!)));
        await using var command = new SqlCommand($"SELECT {columns} FROM dbo.DealerSettings WHERE MemberCode = @memberCode;", connection);
        command.Parameters.AddWithValue("@memberCode", memberCode!);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new SettingsException("MemberCodeMissing", "The configured member record is no longer available.");
        for (var index = 0; index < available.Length; index++)
        {
            var normalized = Normalize(reader.GetValue(index), reader.GetFieldType(index), columnTypes[available[index].Mapping.ColumnCandidate!]);
            values[available[index].ParameterId] = DealerSettingValueCodec.ToApiValue(available[index].ParameterId, normalized);
        }
        return values;
    }

    private static object? Normalize(object value, Type fieldType, string sqlType)
    {
        if (value is DBNull) return null;
        if (string.Equals(sqlType, "bigint", StringComparison.OrdinalIgnoreCase)) return Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        if (fieldType == typeof(bool)) return value;
        if (fieldType == typeof(string)) return value;
        if (fieldType == typeof(byte) || fieldType == typeof(short) || fieldType == typeof(int)) return Convert.ToString(value, CultureInfo.InvariantCulture);
        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private SettingsResponse BuildResponse(IReadOnlyDictionary<string, object?> values, string warning)
    {
        var items = catalog.AllSettings.Select(definition => new SettingItem(
            definition.ParameterId,
            values.GetValueOrDefault(definition.ParameterId),
            definition.Mapping.Status,
            definition.Mapping.Status == "verified" && definition.ControlType != "file")).ToArray();
        return new SettingsResponse("SqlServer", items, warning);
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Any(character => !(char.IsLetterOrDigit(character) || character == '_')))
            throw new SettingsException("ValidationFailed", "The server-side catalog contains an invalid database identifier.");
        return $"[{identifier}]";
    }
}
