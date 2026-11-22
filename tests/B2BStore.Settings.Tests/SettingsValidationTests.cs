using System.Text.Json;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public class SettingsValidationTests
{
    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));

    [Fact]
    public void InventoryCatalogKeepsAllControlsUnverified()
    {
        var catalog = Catalog;
        Assert.Equal(98, catalog.ControlCount);
        Assert.Equal(93, catalog.StatusCounts["candidate"]);
        Assert.Equal(5, catalog.StatusCounts["unresolved"]);
        Assert.All(catalog.AllSettings, setting => Assert.NotEqual("verified", setting.Mapping.Status));
    }

    [Fact]
    public void UnknownFieldIsRejected()
    {
        var request = new UpdateRequest([Change("injected-field", null, "bad")]);
        Assert.Throws<SettingsException>(() => SettingUpdateValidator.ValidateAndProject(Catalog, request, new Dictionary<string, object?>(), true));
    }

    [Fact]
    public void CandidateMappingIsRejectedForSqlWrites()
    {
        var catalog = Catalog;
        var setting = catalog.AllSettings.First(item => item.ControlType == "select" && item.Options.Count > 0);
        var values = catalog.AllSettings.ToDictionary(item => item.ParameterId, _ => (object?)null);
        var request = new UpdateRequest([Change(setting.ParameterId, null, setting.Options[0].Value)]);
        var error = Assert.Throws<SettingsException>(() => SettingUpdateValidator.ValidateAndProject(catalog, request, values, false));
        Assert.Equal("MappingNotVerified", error.Code);
    }

    [Fact]
    public async Task MockStoreRejectsAllWrites()
    {
        var catalog = Catalog;
        var store = new MockSettingsStore(catalog);
        var setting = catalog.AllSettings.First(item => item.ControlType == "select" && item.Options.Count > 0);
        var update = new UpdateRequest([Change(setting.ParameterId, null, setting.Options[0].Value)]);
        var error = await Assert.ThrowsAsync<SettingsException>(() => store.UpdateAsync(update, CancellationToken.None));
        Assert.Equal("ReadOnlyMode", error.Code);
    }

    [Fact]
    public void DuplicateAndInvalidSelectUpdatesAreRejected()
    {
        var catalog = Catalog;
        var setting = catalog.AllSettings.First(item => item.ControlType == "select" && item.Options.Count > 0);
        var values = catalog.AllSettings.ToDictionary(item => item.ParameterId, _ => (object?)null);
        var duplicate = new UpdateRequest([Change(setting.ParameterId, null, setting.Options[0].Value), Change(setting.ParameterId, null, setting.Options[0].Value)]);
        Assert.Throws<SettingsException>(() => SettingUpdateValidator.ValidateAndProject(catalog, duplicate, values, true));
        var invalid = new UpdateRequest([Change(setting.ParameterId, null, "not-an-option")]);
        Assert.Throws<SettingsException>(() => SettingUpdateValidator.ValidateAndProject(catalog, invalid, values, true));
    }

    [Fact]
    public void MissingOrAmbiguousMemberContextBlocksSelection()
    {
        Assert.Equal("MemberCodeMissing", Assert.Throws<SettingsException>(() => RowSelectionGuard.EnsureSingleTarget(0)).Code);
        Assert.Throws<SettingsException>(() => RowSelectionGuard.EnsureSingleTarget(2));
        RowSelectionGuard.EnsureSingleTarget(1);
    }

    [Fact]
    public void BigintSerializesAsAnExactDecimalString()
    {
        const string value = "4611686018427387904";
        var json = JsonSerializer.Serialize(new SettingItem("id", value, "candidate", false), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.String, document.RootElement.GetProperty("value").ValueKind);
        Assert.Equal(value, document.RootElement.GetProperty("value").GetString());
    }

    [Fact]
    public void SqlBigintParameterUsesExactLongValue()
    {
        const string value = "4611686018427387904";
        var parameter = SqlValueConverter.CreateParameter("@setting", "bigint", value);
        Assert.Equal(System.Data.SqlDbType.BigInt, parameter.SqlDbType);
        Assert.Equal(1L << 62, parameter.Value);
    }

    [Fact]
    public void SqlBigintNullUsesTypedDatabaseNull()
    {
        var parameter = SqlValueConverter.CreateParameter("@setting", "bigint", null);
        Assert.Equal(System.Data.SqlDbType.BigInt, parameter.SqlDbType);
        Assert.Equal(DBNull.Value, parameter.Value);
    }

    private static SettingChange Change(string id, string? expected, string? value) => new(
        id,
        JsonDocument.Parse(expected is null ? "null" : JsonSerializer.Serialize(expected)).RootElement,
        value is null ? JsonDocument.Parse("null").RootElement : JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement,
        null);
}
