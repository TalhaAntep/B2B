using System.Text.Json;

namespace B2BStore.Settings.Api;

public sealed record ApiError(string Code, string Message);
public sealed record SettingItem(string ParameterId, object? Value, string MappingStatus, bool Editable);
public sealed record SettingsResponse(string Mode, IReadOnlyList<SettingItem> Items, string? Warning = null, bool TestWritesEnabled = false);
public sealed record BitOperation(string OptionValue, bool Enabled);
public sealed record SettingChange(
    string ParameterId,
    JsonElement ExpectedValue,
    // JsonElement preserves the distinction between an omitted (Undefined) JSON property
    // and an explicitly supplied `null` (Null) property during deserialization.
    JsonElement Value,
    IReadOnlyList<BitOperation>? BitOperations);
public sealed record UpdateRequest(IReadOnlyList<SettingChange> Changes);
public sealed record SiteStatusRollbackChange(int? ExpectedValue, int NewValue);
public sealed record ParameterOrderRollbackChange(string? ExpectedValue, IReadOnlyList<BitOperation> Operations);
public sealed record RollbackOnlySettingsTestRequest(
    SiteStatusRollbackChange? SiteStatus,
    ParameterOrderRollbackChange? ParameterOrder);
public sealed record RollbackOnlySettingsTestResponse(
    int? SiteStatusBefore,
    int? SiteStatusInsideTransaction,
    int? SiteStatusAfterRollback,
    string? ParameterOrderBefore,
    string? ParameterOrderInsideTransaction,
    string? ParameterOrderAfterRollback,
    bool RolledBack);
public sealed record PersistentSiteStatusChange(int? ExpectedValue, int NewValue);
public sealed record PersistentParameterOrderChange(string? ExpectedValue, IReadOnlyList<BitOperation> Operations);
public sealed record PersistentSettingsSaveRequest(
    PersistentSiteStatusChange? SiteStatus,
    PersistentParameterOrderChange? ParameterOrder,
    PersistentLayoutChange? Layout = null,
    PersistentMenuVisibilityChange? DontShowMenuAtDealers = null,
    PersistentBooleanChange? DeliveryDateShow = null,
    IReadOnlyList<SettingChange>? Changes = null);
public sealed record PersistentLayoutChange(int? ExpectedValue, int NewValue);
public sealed record PersistentMenuVisibilityChange(int? ExpectedValue, int NewValue);
public sealed record PersistentBooleanChange(bool? ExpectedValue, bool NewValue);
public sealed record PersistentSettingsSaveResult(
    int? SiteStatus,
    string? ParameterOrder,
    bool Committed,
    int? Layout = null,
    int? DontShowMenuAtDealers = null,
    bool? DeliveryDateShow = null,
    IReadOnlyDictionary<string, object?>? Values = null);

public sealed class SettingsException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record ParameterOption(string Value, string Label);
public sealed record ParameterMapping(
    string Status,
    string SourceStatus,
    string? TableCandidate,
    string? ColumnCandidate,
    string? SqlDataType,
    bool? Nullable,
    string Evidence,
    string? OutstandingQuestion);
public sealed record ParameterDefinition(
    string ParameterId,
    string OriginalLabel,
    string Panel,
    string Category,
    string HtmlControlName,
    string HtmlControlId,
    string HtmlElement,
    string ControlType,
    bool Multiple,
    IReadOnlyList<ParameterOption> Options,
    ParameterMapping Mapping,
    string ValueSemantics);
public sealed record ParameterGroup(string Panel, string Section, IReadOnlyList<ParameterDefinition> Controls);
public sealed record ParameterCatalog(
    int SchemaVersion,
    string Title,
    string SourceInventory,
    string SourceMapping,
    string EvidenceNote,
    int ControlCount,
    IReadOnlyDictionary<string, int> StatusCounts,
    IReadOnlyList<ParameterGroup> Groups)
{
    public IReadOnlyList<ParameterDefinition> AllSettings => Groups.SelectMany(group => group.Controls).ToArray();

    public static ParameterCatalog Load(string path)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var catalog = JsonSerializer.Deserialize<ParameterCatalog>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("The parameter catalog is empty.");
        if (catalog.ControlCount != catalog.AllSettings.Count)
            throw new InvalidOperationException("The parameter catalog count does not match its entries.");
        return catalog;
    }
}

public interface ISettingsStore
{
    Task<SettingsResponse> ReadAsync(CancellationToken cancellationToken);
    Task<SettingsResponse> UpdateAsync(UpdateRequest request, CancellationToken cancellationToken);
}
