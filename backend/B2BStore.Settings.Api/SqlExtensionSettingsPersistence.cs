using System.Data;
using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace B2BStore.Settings.Api;

/// <summary>Fixed application-owned values for controls with no demonstrated legacy destination.</summary>
public sealed class SqlExtensionSettingsPersistence(
    ParameterCatalog catalog,
    string? connectionString,
    string? memberCode,
    bool writeModeEnabled)
{
    public const string ProductSearchId = DealerSettingValueCodec.ParameterProductSearchParameterId;
    public const string ProductSearchHighKey = "ParameterProductSearch.HighMask";
    public const string ProductDetailNotLogin2Id = "ctl00_PageContent_ddlParameterProductDetailNotLogin2";
    public const string ParameterMenu2Id = "ctl00_PageContent_ddlParameterMenu2";
    public const string WhatsAppOrderNotificationId = "ctl00_PageContent_chkWhatsAppOrderNotification";
    public const string FavoriteIconPositionId = "ctl00_PageContent_rbFavoriteIconLeft";
    public const string FavoriteIconVerticalPositionId = "ctl00_PageContent_rbFavoriteIconTop";

    private const string TargetMemberCode = "store5";
    private const string TargetDatabase = "Store5_Settings_Test";

    private sealed record ExtensionDefinition(string ParameterId, string Key, string ValueType);
    private static readonly ExtensionDefinition[] ExtensionDefinitions =
    [
        new(ProductDetailNotLogin2Id, "ParameterProductDetailNotLogin2", "bitmask64"),
        new(ParameterMenu2Id, "ParameterMenu2", "bitmask64"),
        new(WhatsAppOrderNotificationId, "WhatsAppOrderNotification", "boolean"),
        new(FavoriteIconPositionId, "FavoriteIconPosition", "enum"),
        new(FavoriteIconVerticalPositionId, "FavoriteIconVerticalPosition", "enum"),
    ];

    private static readonly IReadOnlyDictionary<string, ExtensionDefinition> ByParameterId =
        ExtensionDefinitions.ToDictionary(item => item.ParameterId, StringComparer.Ordinal);
    private static readonly IReadOnlyDictionary<string, ExtensionDefinition> ByKey =
        ExtensionDefinitions.Append(new ExtensionDefinition(ProductSearchId, ProductSearchHighKey, "bitmask64"))
            .ToDictionary(item => item.Key, StringComparer.Ordinal);

    public static bool IsSupportedWrite(string parameterId) => parameterId == ProductSearchId || ByParameterId.ContainsKey(parameterId);

    public static object? DecodeApplicationExtensionValue(ParameterCatalog catalog, string parameterId, string? storedValue)
    {
        if (!ByParameterId.TryGetValue(parameterId, out var spec))
            throw new SettingsException("MappingNotVerified", "The extension setting is not explicitly allowlisted.");
        var definition = catalog.AllSettings.Single(item => item.ParameterId == parameterId);
        return FromStoredValue(definition, spec.ValueType, storedValue);
    }

    public static string EncodeApplicationExtensionValue(ParameterCatalog catalog, string parameterId, object? value)
    {
        if (!ByParameterId.TryGetValue(parameterId, out _))
            throw new SettingsException("MappingNotVerified", "The extension setting is not explicitly allowlisted.");
        var definition = catalog.AllSettings.Single(item => item.ParameterId == parameterId);
        return ToStoredValue(definition, value);
    }

    public static async Task ApplyReadValuesAsync(ParameterCatalog catalog, SqlConnection connection,
        IDictionary<string, object?> values, string memberCode, CancellationToken cancellationToken)
    {
        if (!string.Equals(memberCode, TargetMemberCode, StringComparison.Ordinal))
            throw new SettingsException("DatabaseIsolationViolation", "Extension settings are restricted to the configured store5 record.");
        var stored = await ReadStoredValuesAsync(connection, null, memberCode, cancellationToken);
        foreach (var definition in catalog.AllSettings.Where(item => ByParameterId.ContainsKey(item.ParameterId)))
        {
            var spec = ByParameterId[definition.ParameterId];
            stored.TryGetValue(spec.Key, out var raw);
            values[definition.ParameterId] = FromStoredValue(definition, spec.ValueType, raw);
        }

        var searchDefinition = catalog.AllSettings.SingleOrDefault(item => item.ParameterId == ProductSearchId);
        if (searchDefinition is not null && values.TryGetValue(ProductSearchId, out var lowRaw))
        {
            stored.TryGetValue(ProductSearchHighKey, out var highRaw);
            var low = lowRaw is null ? 0L : ParseBitmask(lowRaw);
            var high = highRaw is null ? 0L : ParseBitmask(highRaw);
            values[ProductSearchId] = lowRaw is null && highRaw is null
                ? null
                : (low | high).ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Reads only the configured member's known extension keys. Missing rows stay absent/null.</summary>
    public static async Task<IReadOnlyDictionary<string, string?>> ReadStoredValuesAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string memberCode,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, transaction, cancellationToken)) return new Dictionary<string, string?>(StringComparer.Ordinal);
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        await using var command = new SqlCommand(
            "SELECT [SettingKey],[SettingValue],[ValueType] FROM dbo.DealerSystemSettingExtension WHERE MemberCode=@memberCode AND SettingKey IN (N'ParameterProductSearch.HighMask',N'ParameterProductDetailNotLogin2',N'ParameterMenu2',N'WhatsAppOrderNotification',N'FavoriteIconPosition',N'FavoriteIconVerticalPosition');",
            connection, transaction);
        command.Parameters.AddWithValue("@memberCode", memberCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = reader.GetString(0);
            if (!ByKey.TryGetValue(key, out var definition)) continue;
            var value = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (!string.Equals(reader.GetString(2), definition.ValueType, StringComparison.Ordinal))
                throw new SettingsException("DatabaseIsolationViolation", "The application extension value type does not match its fixed setting definition.");
            result[key] = value;
        }
        return result;
    }

    public async Task<IReadOnlyDictionary<string, object?>> SaveAsync(
        IReadOnlyList<SettingChange> changes,
        bool authenticated,
        CancellationToken cancellationToken)
    {
        RollbackWriteTestGuard.EnsureAllowed(writeModeEnabled, authenticated, memberCode, connectionString);
        if (changes.Count == 0 || changes.Select(change => change.ParameterId).Distinct(StringComparer.Ordinal).Count() != changes.Count
            || changes.Any(change => !IsSupportedWrite(change.ParameterId)))
            throw new SettingsException("MappingNotVerified", "The request contains a setting outside the fixed extension allowlist.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new SettingsException("DatabaseNotConfigured", "The approved local test database could not be reached.");

        var definitions = catalog.AllSettings.ToDictionary(item => item.ParameterId, StringComparer.Ordinal);
        foreach (var change in changes) ValidateControl(definitions[change.ParameterId], change);

        await using var connection = new SqlConnection(connectionString);
        try { await connection.OpenAsync(cancellationToken); }
        catch (SqlException) { throw new SettingsException("DatabaseNotConfigured", "The approved local test database could not be reached."); }
        await EnsureDatabaseAsync(connection, cancellationToken);
        await EnsureTargetRowAsync(connection, null, cancellationToken);
        await EnsureExtensionSchemaAsync(connection, cancellationToken);
        await EnsureLegacyColumnsAsync(connection, cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var committed = false;
        Dictionary<string, object?> expectedResults = new(StringComparer.Ordinal);
        try
        {
            await EnsureTargetRowAsync(connection, transaction, cancellationToken);
            var current = await ReadCurrentValuesAsync(connection, transaction, changes, cancellationToken);
            expectedResults = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var change in changes)
            {
                var currentValue = current[change.ParameterId];
                if (!SettingUpdateValidator.ValuesEqual(currentValue, SettingUpdateValidator.ReadExpected(change.ExpectedValue)))
                    throw new SettingsException("ConcurrencyConflict", "A setting changed before the save.");
                expectedResults[change.ParameterId] = ComputeNewValue(definitions[change.ParameterId], change, currentValue);
            }

            foreach (var change in changes)
            {
                var value = expectedResults[change.ParameterId];
                if (change.ParameterId == ProductSearchId)
                    await SaveProductSearchAsync(connection, transaction, change, current[change.ParameterId], value, cancellationToken);
                else
                {
                    var definition = ByParameterId[change.ParameterId];
                    var persisted = ToStoredValue(definitions[change.ParameterId], value);
                    await UpsertExtensionAsync(connection, transaction, definition, change.ExpectedValue, persisted, cancellationToken, compareCurrent: false);
                }
            }

            var inside = await ReadCurrentValuesAsync(connection, transaction, changes, cancellationToken);
            foreach (var change in changes)
                if (!SettingUpdateValidator.ValuesEqual(inside[change.ParameterId], expectedResults[change.ParameterId]))
                    throw new SettingsException("ReadbackMismatch", "The isolated extension setting did not match its requested value.");
            await transaction.CommitAsync(cancellationToken);
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch (SqlException) { throw new SettingsException("RollbackVerificationFailed", "The isolated settings transaction could not be rolled back safely."); }
            }
        }

        var afterCommit = await ReadCurrentValuesAsync(connection, null, changes, cancellationToken);
        foreach (var change in changes)
            if (!SettingUpdateValidator.ValuesEqual(afterCommit[change.ParameterId], expectedResults[change.ParameterId]))
                throw new SettingsException("ReadbackMismatch", "A committed isolated extension setting could not be verified.");
        return afterCommit;
    }

    private async Task<IReadOnlyDictionary<string, object?>> ReadCurrentValuesAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        IReadOnlyList<SettingChange> changes,
        CancellationToken token)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        var needsSearch = changes.Any(change => change.ParameterId == ProductSearchId);
        string? searchLow = null;
        if (needsSearch)
        {
            await using var command = new SqlCommand(
                "SELECT [ParameterProductSearch] FROM dbo.DealerSettings WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode;", connection, transaction);
            command.Parameters.AddWithValue("@memberCode", TargetMemberCode);
            var value = await command.ExecuteScalarAsync(token);
            searchLow = value is null or DBNull ? null : Convert.ToString(DealerSettingValueCodec.ToApiValue(ProductSearchId, value), CultureInfo.InvariantCulture);
        }

        var extensionValues = await ReadStoredValuesForUpdateAsync(connection, transaction, changes, token);
        foreach (var change in changes)
        {
            if (change.ParameterId == ProductSearchId)
            {
                extensionValues.TryGetValue(ProductSearchHighKey, out var highValue);
                var high = highValue is null ? 0L : ParseBitmask(highValue);
                var low = searchLow is null ? 0L : long.Parse(searchLow, NumberStyles.None, CultureInfo.InvariantCulture);
                var combined = low | high;
                result[change.ParameterId] = searchLow is null && highValue is null
                    ? null
                    : combined.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var spec = ByParameterId[change.ParameterId];
                extensionValues.TryGetValue(spec.Key, out var stored);
                result[change.ParameterId] = FromStoredValue(catalog.AllSettings.Single(x => x.ParameterId == change.ParameterId), spec.ValueType, stored);
            }
        }
        return result;
    }

    private async Task<IReadOnlyDictionary<string, string?>> ReadStoredValuesForUpdateAsync(
        SqlConnection connection, SqlTransaction? transaction, IReadOnlyList<SettingChange> changes, CancellationToken token)
    {
        if (!await TableExistsAsync(connection, transaction, token))
            throw new SettingsException("DatabaseIsolationViolation", "Apply the explicit extension-table migration to Store5_Settings_Test before enabling these settings.");
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var keys = changes.Select(change => change.ParameterId == ProductSearchId ? ProductSearchHighKey : ByParameterId[change.ParameterId].Key)
            .Distinct(StringComparer.Ordinal).ToArray();
        await using var command = new SqlCommand(
            $"SELECT [SettingKey],[SettingValue],[ValueType] FROM dbo.DealerSystemSettingExtension WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode AND SettingKey IN ({string.Join(',', keys.Select((_, i) => $"@key{i}"))});",
            connection, transaction);
        command.Parameters.AddWithValue("@memberCode", TargetMemberCode);
        for (var i = 0; i < keys.Length; i++) command.Parameters.AddWithValue($"@key{i}", keys[i]);
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var key = reader.GetString(0);
            if (!ByKey.TryGetValue(key, out var definition) || !string.Equals(reader.GetString(2), definition.ValueType, StringComparison.Ordinal))
                throw new SettingsException("DatabaseIsolationViolation", "An application extension row has an unsupported key or value type.");
            values[key] = reader.IsDBNull(1) ? null : reader.GetString(1);
        }
        return values;
    }

    private static object? ComputeNewValue(ParameterDefinition definition, SettingChange change, object? currentValue)
    {
        if (definition.ControlType == "bitmask")
        {
            if (change.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) || change.BitOperations is null)
                throw new SettingsException("ValidationFailed", "A multi-select setting requires bit operations only.");
            var allowed = definition.Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
            ValidatePowerOfTwoOptions(definition);
            var current = currentValue is null ? 0L : ParseBitmask(currentValue);
            return BitmaskEditor.Apply(current, change.BitOperations, allowed).ToString(CultureInfo.InvariantCulture);
        }
        if (definition.ControlType == "checkbox")
        {
            if (change.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || change.BitOperations is { Count: > 0 })
                throw new SettingsException("ValidationFailed", "The extension checkbox requires an explicit Boolean value.");
            return change.Value.GetBoolean();
        }
        if (definition.ControlType == "radio")
        {
            if (change.Value.ValueKind != JsonValueKind.String || change.BitOperations is { Count: > 0 })
                throw new SettingsException("ValidationFailed", "The extension position requires one listed radio option.");
            var selected = change.Value.GetString();
            if (!definition.Options.Any(option => option.Value == selected))
                throw new SettingsException("ValidationFailed", "The extension position must use a listed source option.");
            return selected;
        }
        throw new SettingsException("MappingNotVerified", "The extension control type is not supported.");
    }

    private static object? FromStoredValue(ParameterDefinition definition, string valueType, string? value)
    {
        if (value is null) return null;
        return valueType switch
        {
            "bitmask64" => ParseBitmask(value).ToString(CultureInfo.InvariantCulture),
            "boolean" => value switch { "true" => true, "false" => false, _ => throw new SettingsException("DatabaseIsolationViolation", "An extension Boolean value is malformed.") },
            "enum" => definition.Options.SingleOrDefault(option => CanonicalEnum(definition.ParameterId, option.Value) == value)?.Value
                ?? throw new SettingsException("DatabaseIsolationViolation", "An extension enum value is not in the source control options."),
            _ => throw new SettingsException("DatabaseIsolationViolation", "An extension value uses an unsupported type."),
        };
    }

    private static string ToStoredValue(ParameterDefinition definition, object? value)
    {
        var spec = ByParameterId[definition.ParameterId];
        if (spec.ValueType == "bitmask64") return ParseBitmask(value).ToString(CultureInfo.InvariantCulture);
        if (spec.ValueType == "boolean" && value is bool boolean) return boolean ? "true" : "false";
        if (spec.ValueType == "enum" && value is string sourceValue) return CanonicalEnum(definition.ParameterId, sourceValue);
        throw new SettingsException("ValidationFailed", "The extension value does not match its fixed storage type.");
    }

    private async Task SaveProductSearchAsync(SqlConnection connection, SqlTransaction transaction, SettingChange change,
        object? before, object? after, CancellationToken token)
    {
        var originalLowApi = before is null ? null : Convert.ToString(before, CultureInfo.InvariantCulture);
        // The SQL low word has its own concurrency predicate; derive it from the API mask's low word.
        var expectedFull = SettingUpdateValidator.ReadExpected(change.ExpectedValue);
        var expectedFullMask = expectedFull is null ? 0L : ParseBitmask(expectedFull);
        var expectedLow = unchecked((short)(ushort)(expectedFullMask & ushort.MaxValue));
        var next = ParseBitmask(after);
        var low = unchecked((short)(ushort)(next & ushort.MaxValue));
        var high = next & ~((long)ushort.MaxValue);
        await using (var update = new SqlCommand(
            "UPDATE dbo.DealerSettings SET [ParameterProductSearch]=@newValue WHERE MemberCode=@memberCode AND (([ParameterProductSearch]=@expectedValue) OR ([ParameterProductSearch] IS NULL AND @expectedWasNull=1));",
            connection, transaction))
        {
            update.Parameters.Add(SqlValueConverter.CreateParameter("@newValue", "smallint", low));
            update.Parameters.Add(SqlValueConverter.CreateParameter("@expectedValue", "smallint", expectedLow));
            update.Parameters.AddWithValue("@expectedWasNull", originalLowApi is null);
            update.Parameters.AddWithValue("@memberCode", TargetMemberCode);
            if (await update.ExecuteNonQueryAsync(token) != 1) throw new SettingsException("ConcurrencyConflict", "Product search setting changed before the save.");
        }
        var highSpec = ByKey[ProductSearchHighKey];
        await UpsertExtensionAsync(connection, transaction, highSpec, JsonElementFromExpected(
            (next & ~((long)ushort.MaxValue)) == 0 && expectedFull is null ? null : high.ToString(CultureInfo.InvariantCulture)),
            high.ToString(CultureInfo.InvariantCulture), token, compareCurrent: false);
    }

    private async Task UpsertExtensionAsync(SqlConnection connection, SqlTransaction transaction, ExtensionDefinition definition,
        JsonElement expectedJson, string value, CancellationToken token, bool compareCurrent = true)
    {
        var expected = expectedJson.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? null
            : expectedJson.ValueKind == JsonValueKind.String ? expectedJson.GetString()
            : expectedJson.ValueKind switch { JsonValueKind.True => "true", JsonValueKind.False => "false", _ => expectedJson.GetRawText() };
        var currentValue = await ReadOneExtensionAsync(connection, transaction, definition.Key, token);
        if (compareCurrent && !string.Equals(currentValue, expected, StringComparison.Ordinal))
            throw new SettingsException("ConcurrencyConflict", "An application extension setting changed before the save.");
        if (currentValue is null)
        {
            await using var insert = new SqlCommand(
                "INSERT dbo.DealerSystemSettingExtension (MemberCode,SettingKey,SettingValue,ValueType,UpdatedAtUtc) VALUES (@memberCode,@settingKey,@settingValue,@valueType,SYSUTCDATETIME());",
                connection, transaction);
            AddExtensionParameters(insert, definition, value);
            await insert.ExecuteNonQueryAsync(token);
        }
        else
        {
            await using var update = new SqlCommand(
                "UPDATE dbo.DealerSystemSettingExtension SET SettingValue=@settingValue,UpdatedAtUtc=SYSUTCDATETIME() WHERE MemberCode=@memberCode AND SettingKey=@settingKey AND ValueType=@valueType AND SettingValue=@expectedValue;",
                connection, transaction);
            AddExtensionParameters(update, definition, value);
            update.Parameters.AddWithValue("@expectedValue", currentValue);
            if (await update.ExecuteNonQueryAsync(token) != 1) throw new SettingsException("ConcurrencyConflict", "An application extension setting changed before the save.");
        }
    }

    private static void AddExtensionParameters(SqlCommand command, ExtensionDefinition definition, string value)
    {
        command.Parameters.AddWithValue("@memberCode", TargetMemberCode);
        command.Parameters.AddWithValue("@settingKey", definition.Key);
        command.Parameters.AddWithValue("@settingValue", value);
        command.Parameters.AddWithValue("@valueType", definition.ValueType);
    }

    private static async Task<string?> ReadOneExtensionAsync(SqlConnection connection, SqlTransaction transaction, string key, CancellationToken token)
    {
        await using var command = new SqlCommand(
            "SELECT SettingValue FROM dbo.DealerSystemSettingExtension WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode AND SettingKey=@settingKey;",
            connection, transaction);
        command.Parameters.AddWithValue("@memberCode", TargetMemberCode);
        command.Parameters.AddWithValue("@settingKey", key);
        var result = await command.ExecuteScalarAsync(token);
        return result is null or DBNull ? null : Convert.ToString(result, CultureInfo.InvariantCulture);
    }

    private static async Task EnsureTargetRowAsync(SqlConnection connection, SqlTransaction? transaction, CancellationToken token)
    {
        await using var command = new SqlCommand("SELECT COUNT_BIG(*) FROM dbo.DealerSettings WITH (UPDLOCK,HOLDLOCK) WHERE MemberCode=@memberCode;", connection, transaction);
        command.Parameters.AddWithValue("@memberCode", TargetMemberCode);
        RowSelectionGuard.EnsureSingleTarget(Convert.ToInt64(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture));
    }

    private static async Task EnsureDatabaseAsync(SqlConnection connection, CancellationToken token)
    {
        await using var command = new SqlCommand("SELECT DB_NAME();", connection);
        var database = Convert.ToString(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
        if (!string.Equals(database, TargetDatabase, StringComparison.Ordinal))
            throw new SettingsException("DatabaseIsolationViolation", "The connected database is not the approved isolated test database.");
    }

    private static async Task EnsureLegacyColumnsAsync(SqlConnection connection, CancellationToken token)
    {
        const string sql = "SELECT c.name,t.name,c.max_length,c.is_nullable FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID(N'dbo.DealerSettings') AND c.name IN (N'ParameterProductSearch');";
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token) || reader.GetString(0) != "ParameterProductSearch" || reader.GetString(1) != "smallint"
            || reader.GetInt16(2) != 2 || !reader.GetBoolean(3))
            throw new SettingsException("DatabaseIsolationViolation", "The approved database does not have the expected ParameterProductSearch smallint column.");
    }

    private static async Task EnsureExtensionSchemaAsync(SqlConnection connection, CancellationToken token)
    {
        if (!await TableExistsAsync(connection, null, token))
            throw new SettingsException("DatabaseIsolationViolation", "Apply the explicit extension-table migration to Store5_Settings_Test before enabling these settings.");
        const string sql = "SELECT c.name,t.name,c.max_length,c.is_nullable FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id WHERE c.object_id=OBJECT_ID(N'dbo.DealerSystemSettingExtension');";
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(token);
        var found = new Dictionary<string, (string Type, short Length, bool Nullable)>(StringComparer.Ordinal);
        while (await reader.ReadAsync(token)) found[reader.GetString(0)] = (reader.GetString(1), reader.GetInt16(2), reader.GetBoolean(3));
        if (!found.TryGetValue("MemberCode", out var member) || member != ("nvarchar", (short)200, false)
            || !found.TryGetValue("SettingKey", out var key) || key.Type != "nvarchar" || key.Length != 256 || key.Nullable
            || !found.TryGetValue("SettingValue", out var value) || value.Type != "nvarchar" || value.Length != 256 || value.Nullable
            || !found.TryGetValue("ValueType", out var valueType) || valueType.Type != "varchar" || valueType.Length != 24 || valueType.Nullable
            || !found.TryGetValue("UpdatedAtUtc", out var updated) || updated.Type != "datetime2" || updated.Nullable)
            throw new SettingsException("DatabaseIsolationViolation", "The application extension table schema does not match the approved migration.");
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, SqlTransaction? transaction, CancellationToken token)
    {
        await using var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo.DealerSystemSettingExtension',N'U') IS NULL THEN 0 ELSE 1 END;", connection, transaction);
        return Convert.ToInt32(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture) == 1;
    }

    private static void ValidateControl(ParameterDefinition definition, SettingChange change)
    {
        if (definition.ParameterId == ProductSearchId)
        {
            if (definition.ControlType != "bitmask" || !definition.Multiple || definition.Mapping.ColumnCandidate != "ParameterProductSearch"
                || definition.Mapping.SqlDataType != "smallint")
                throw new SettingsException("MappingNotVerified", "Product search control no longer matches the split 16-bit mapping.");
            ValidatePowerOfTwoOptions(definition);
            if (change.BitOperations is null || change.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                throw new SettingsException("ValidationFailed", "Product search updates require option operations.");
            return;
        }
        if (!ByParameterId.TryGetValue(definition.ParameterId, out var spec))
            throw new SettingsException("MappingNotVerified", "The extension setting is not explicitly allowlisted.");
        if (spec.ValueType == "bitmask64")
        {
            if (definition.ControlType != "bitmask" || !definition.Multiple || change.BitOperations is null
                || change.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined))
                throw new SettingsException("ValidationFailed", "The extension multi-select requires option operations.");
            ValidatePowerOfTwoOptions(definition);
        }
        else if (spec.ValueType == "boolean" && definition.ControlType != "checkbox")
            throw new SettingsException("MappingNotVerified", "The extension Boolean control changed unexpectedly.");
        else if (spec.ValueType == "enum" && (definition.ControlType != "radio" || definition.Options.Count != 2))
            throw new SettingsException("MappingNotVerified", "The extension enum control changed unexpectedly.");
    }

    private static void ValidatePowerOfTwoOptions(ParameterDefinition definition)
    {
        foreach (var option in definition.Options)
        {
            if (!long.TryParse(option.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var bit)
                || bit <= 0 || (bit & (bit - 1)) != 0)
                throw new SettingsException("MappingNotVerified", "An extension bitmask option is not a positive power of two.");
        }
    }

    private static long ParseBitmask(object? value)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
            throw new SettingsException("ValidationFailed", "A bitmask must be an unsigned decimal string within the supported 64-bit range.");
        return parsed;
    }

    private static string CanonicalEnum(string parameterId, string sourceValue) => (parameterId, sourceValue) switch
    {
        (FavoriteIconPositionId, "rbFavoriteIconLeft") => "Left",
        (FavoriteIconPositionId, "rbFavoriteIconRight") => "Right",
        (FavoriteIconVerticalPositionId, "rbFavoriteIconTop") => "Top",
        (FavoriteIconVerticalPositionId, "rbFavoriteIconBottom") => "Bottom",
        _ => throw new SettingsException("ValidationFailed", "The favorite icon position is not a supported source option."),
    };

    private static JsonElement JsonElementFromExpected(string? value) => JsonDocument.Parse(value is null ? "null" : JsonSerializer.Serialize(value)).RootElement.Clone();
}
