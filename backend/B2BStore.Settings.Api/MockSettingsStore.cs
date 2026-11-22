namespace B2BStore.Settings.Api;

public sealed class MockSettingsStore(ParameterCatalog catalog) : ISettingsStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, object?> _values = catalog.AllSettings.ToDictionary(item => item.ParameterId, _ => (object?)null, StringComparer.Ordinal);

    public Task<SettingsResponse> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync) return Task.FromResult(CreateResponse());
    }

    public Task<SettingsResponse> UpdateAsync(UpdateRequest request, CancellationToken cancellationToken) =>
        throw new SettingsException("ReadOnlyMode", "Settings are read-only; writes are disabled.");

    private SettingsResponse CreateResponse()
    {
        var items = catalog.AllSettings.Select(definition => new SettingItem(
            definition.ParameterId,
            _values[definition.ParameterId],
            definition.Mapping.Status,
            definition.ControlType != "file")).ToArray();
        return new SettingsResponse("Mock", items, "Mock mode contains synthetic, unset values only; it does not read or write SQL Server data.");
    }
}
