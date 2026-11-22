using System.Globalization;
using System.Text.Json;

namespace B2BStore.Settings.Api;

public static class SettingUpdateValidator
{
    public static IReadOnlyDictionary<string, object?> ValidateAndProject(
        ParameterCatalog catalog,
        UpdateRequest request,
        IReadOnlyDictionary<string, object?> currentValues,
        bool allowCandidateMappings)
    {
        if (request.Changes is null || request.Changes.Count == 0)
            throw new SettingsException("ValidationFailed", "At least one setting change is required.");
        if (request.Changes.Select(change => change.ParameterId).Distinct(StringComparer.Ordinal).Count() != request.Changes.Count)
            throw new SettingsException("ValidationFailed", "A setting may appear only once in an update request.");
        var definitions = catalog.AllSettings.ToDictionary(item => item.ParameterId, StringComparer.Ordinal);
        var projected = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var change in request.Changes)
        {
            if (!definitions.TryGetValue(change.ParameterId, out var definition))
                throw new SettingsException("ValidationFailed", "A setting key is not present in the server catalog.");
            if (!allowCandidateMappings && definition.Mapping.Status != "verified")
                throw new SettingsException("MappingNotVerified", "This setting mapping is not verified for database writes.");
            if (HasNoRequestedValue(change)) continue;
            if (definition.ControlType == "file")
                throw new SettingsException("ValidationFailed", "File settings are disabled until their storage behavior is verified.");
            if (!currentValues.TryGetValue(change.ParameterId, out var current))
                throw new SettingsException("ValidationFailed", "The current setting value is unavailable.");
            var expected = ReadExpected(change.ExpectedValue);
            if (!ValuesEqual(current, expected))
                throw new SettingsException("ConcurrencyConflict", "The setting changed since it was loaded. Reload before saving.");

            if (definition.ParameterId == "ctl00_PageContent_ddlSchedule")
            {
                if (definition.ControlType != "bitmask" || !definition.Multiple
                    || !definition.Options.Select(option => option.Value).SequenceEqual(["1"], StringComparer.Ordinal))
                    throw new SettingsException("MappingNotVerified", "Schedule does not match its single-option scalar serializer.");
                var explicitNull = change.Value.ValueKind == JsonValueKind.Null
                    && (change.BitOperations is null || change.BitOperations.Count == 0);
                if (explicitNull)
                {
                    projected[change.ParameterId] = null;
                    continue;
                }
                if (change.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                    || change.BitOperations is not { Count: 1 } operations || operations[0].OptionValue != "1")
                    throw new SettingsException("ValidationFailed", "Schedule updates require its one source option, with no bitmask arithmetic.");
                var currentText = Convert.ToString(current, CultureInfo.InvariantCulture);
                if (current is not null && currentText is not ("0" or "1"))
                    throw new SettingsException("ValidationFailed", "The existing Schedule value is outside the consumer-supported scalar range.");
                projected[change.ParameterId] = operations[0].Enabled ? "1" : "0";
                continue;
            }

            if (definition.ControlType == "bitmask")
            {
                if (change.Value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                    || change.BitOperations is null || change.BitOperations.Count == 0)
                    throw new SettingsException("ValidationFailed", "Bitmask changes must contain option operations only.");
                var allowed = definition.Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
                var currentMask = ParseNullableLong(current);
                projected[change.ParameterId] = BitmaskEditor.Apply(currentMask, change.BitOperations, allowed).ToString(CultureInfo.InvariantCulture);
                continue;
            }

            if (change.BitOperations is { Count: > 0 })
                throw new SettingsException("ValidationFailed", "Bit operations are only valid for bitmask settings.");
            if (change.Value.ValueKind == JsonValueKind.Undefined)
                throw new SettingsException("ValidationFailed", "A scalar setting value must be supplied.");
            projected[change.ParameterId] = ValidateValue(definition, change.Value);
        }
        return projected;
    }

    public static bool HasNoRequestedValue(SettingChange change) =>
        change.Value.ValueKind == JsonValueKind.Undefined
        && (change.BitOperations is null || change.BitOperations.Count == 0);

    public static object? ReadExpected(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => throw new SettingsException("ValidationFailed", "An expected value is required for concurrency validation."),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.GetRawText(),
            _ => throw new SettingsException("ValidationFailed", "Expected value must be a scalar or null."),
        };
    }

    public static bool ValuesEqual(object? current, object? expected)
    {
        if (current is null || expected is null) return current is null && expected is null;
        if (current is bool currentBool && expected is bool expectedBool) return currentBool == expectedBool;
        return string.Equals(Convert.ToString(current, CultureInfo.InvariantCulture), Convert.ToString(expected, CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static object? ValidateValue(ParameterDefinition definition, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            if (definition.Mapping.Nullable != true)
                throw new SettingsException("ValidationFailed", "This setting does not permit NULL values.");
            return null;
        }
        var element = value;
        if (definition.ControlType == "checkbox")
        {
            if (element.ValueKind is JsonValueKind.True or JsonValueKind.False) return element.GetBoolean();
            throw new SettingsException("ValidationFailed", "Checkbox values must be true or false.");
        }
        if (element.ValueKind != JsonValueKind.String)
            throw new SettingsException("ValidationFailed", "Text and select values must be strings or null.");
        var text = element.GetString() ?? string.Empty;
        if (text.Length > 4000)
            throw new SettingsException("ValidationFailed", "Text values may not exceed 4000 characters.");
        if (definition.ControlType is "select" or "radio"
            && !definition.Options.Any(option => string.Equals(option.Value, text, StringComparison.Ordinal)))
            throw new SettingsException("ValidationFailed", "The selected value is not present in the allowed catalog.");
        return text;
    }

    private static long? ParseNullableLong(object? value)
    {
        if (value is null) return null;
        if (long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        throw new SettingsException("ValidationFailed", "The current bitmask is not a SQL bigint value.");
    }
}
