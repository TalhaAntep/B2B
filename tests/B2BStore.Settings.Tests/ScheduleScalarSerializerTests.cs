using System.Text.Json;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class ScheduleScalarSerializerTests
{
    private const string Id = "ctl00_PageContent_ddlSchedule";
    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));
    private static JsonElement JsonValue(object? value) => JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement.Clone();
    private static JsonElement JsonNull() => JsonDocument.Parse("null").RootElement.Clone();

    [Theory]
    [InlineData(null, true, "1")]
    [InlineData("0", true, "1")]
    [InlineData("1", false, "0")]
    public void SelectedSourceOptionIsSerializedAsScalarTinyintWithoutBitwiseArithmetic(string? current, bool selected, string expected)
    {
        var definition = Catalog.AllSettings.Single(item => item.ParameterId == Id);
        var change = new SettingChange(Id, JsonValue(current), JsonNull(), [new BitOperation("1", selected)]);
        SqlPersistentSettingsTestService.ValidateCatalogChange(definition, "tinyint", false, true, change);
        var projected = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([change]),
            new Dictionary<string, object?> { [Id] = current }, allowCandidateMappings: true);
        Assert.Equal(expected, projected[Id]);
    }

    [Fact]
    public void ExplicitNullCanRestoreNullableScheduleBaseline()
    {
        var change = new SettingChange(Id, JsonValue("1"), JsonNull(), []);
        var definition = Catalog.AllSettings.Single(item => item.ParameterId == Id);
        SqlPersistentSettingsTestService.ValidateCatalogChange(definition, "tinyint", false, true, change);
        var projected = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([change]),
            new Dictionary<string, object?> { [Id] = "1" }, allowCandidateMappings: true);
        Assert.Null(projected[Id]);
    }

    [Fact]
    public void RejectsUnsupportedStoredValuesAndOptions()
    {
        var invalidCurrent = new SettingChange(Id, JsonValue("2"), JsonNull(), [new BitOperation("1", false)]);
        Assert.Throws<SettingsException>(() => SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([invalidCurrent]),
            new Dictionary<string, object?> { [Id] = "2" }, allowCandidateMappings: true));

        var invalidOption = new SettingChange(Id, JsonValue("0"), JsonNull(), [new BitOperation("2", true)]);
        Assert.Throws<SettingsException>(() => SqlPersistentSettingsTestService.ValidateCatalogChange(
            Catalog.AllSettings.Single(item => item.ParameterId == Id), "tinyint", false, true, invalidOption));
    }
}
