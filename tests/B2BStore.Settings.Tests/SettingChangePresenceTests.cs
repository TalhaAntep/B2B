using System.Text.Json;
using Microsoft.Data.SqlClient;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class SettingChangePresenceTests
{
    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));
    private static JsonSerializerOptions JsonOptions => new(JsonSerializerDefaults.Web);

    [Fact]
    public void DeserializationDistinguishesOmittedValueFromExplicitNull()
    {
        var omitted = JsonSerializer.Deserialize<SettingChange>(
            "{\"parameterId\":\"test\",\"expectedValue\":\"baseline\"}", JsonOptions);
        var explicitNull = JsonSerializer.Deserialize<SettingChange>(
            "{\"parameterId\":\"test\",\"expectedValue\":\"baseline\",\"value\":null}", JsonOptions);

        Assert.NotNull(omitted);
        Assert.NotNull(explicitNull);
        Assert.Equal(JsonValueKind.Undefined, omitted.Value.ValueKind);
        Assert.Equal(JsonValueKind.Null, explicitNull.Value.ValueKind);
    }

    [Fact]
    public async Task OmittedScalarValueRequestsNoUpdateAndMissingExpectedOnProvidedValueIsRejected()
    {
        var setting = Find("ctl00_PageContent_txtLayoutColor");
        var current = new Dictionary<string, object?> { [setting.ParameterId] = "baseline" };
        var missingValue = new SettingChange(setting.ParameterId, Json("\"baseline\""), default, null);
        var missingExpected = new SettingChange(setting.ParameterId, default, Json("\"next\""), null);

        var projected = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([missingValue]), current, true);
        Assert.Empty(projected);

        var connection = new SqlConnectionStringBuilder
        {
            DataSource = RollbackWriteTestGuard.TargetDataSource,
            InitialCatalog = RollbackWriteTestGuard.TargetDatabase,
            IntegratedSecurity = true,
        }.ConnectionString;
        var service = new SqlPersistentSettingsTestService(Catalog, connection, RollbackWriteTestGuard.TargetMemberCode, true);
        var noOp = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, Changes: [missingValue]), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", noOp.Code);
        Assert.Equal("ValidationFailed", Assert.Throws<SettingsException>(() =>
            SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([missingExpected]), current, true)).Code);
    }

    [Fact]
    public void ExplicitNullIsAcceptedOnlyForNullableColumn()
    {
        var productCompany = Find("ctl00_PageContent_ddlCommonSystem");
        var nullChange = new SettingChange(productCompany.ParameterId, Json("\"1\""), Json("null"), null);
        SqlPersistentSettingsTestService.ValidateCatalogChange(productCompany, "tinyint", false, true, nullChange);

        var projected = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([nullChange]),
            new Dictionary<string, object?> { [productCompany.ParameterId] = "1" }, true);
        Assert.Null(projected[productCompany.ParameterId]);

        var nonNullable = Find("ctl00_PageContent_ddlSiteStatus");
        var disallowedNull = new SettingChange(nonNullable.ParameterId, Json("\"1\""), Json("null"), null);
        Assert.Equal("ValidationFailed", Assert.Throws<SettingsException>(() =>
            SqlPersistentSettingsTestService.ValidateCatalogChange(nonNullable, "int", false, false, disallowedNull)).Code);
    }

    [Fact]
    public void EmptyStringRemainsDistinctFromNull()
    {
        var setting = Find("ctl00_PageContent_txtLayoutColor");
        var current = new Dictionary<string, object?> { [setting.ParameterId] = "baseline" };
        var empty = new SettingChange(setting.ParameterId, Json("\"baseline\""), Json("\"\""), null);
        var nullValue = new SettingChange(setting.ParameterId, Json("\"baseline\""), Json("null"), null);

        var emptyProjection = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([empty]), current, true);
        var nullProjection = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([nullValue]), current, true);
        Assert.Equal(string.Empty, emptyProjection[setting.ParameterId]);
        Assert.Null(nullProjection[setting.ParameterId]);
        SqlPersistentSettingsTestService.ValidateCatalogChange(setting, "nvarchar", false, true, empty);
        SqlPersistentSettingsTestService.ValidateCatalogChange(setting, "nvarchar", false, true, nullValue);
    }

    [Fact]
    public void NumericZeroAndBooleanFalseAreAcceptedWithoutCoercion()
    {
        var numeric = Find("ctl00_PageContent_txtMinCartCount");
        var zero = new SettingChange(numeric.ParameterId, Json("\"5\""), Json("\"0\""), null);
        SqlPersistentSettingsTestService.ValidateCatalogChange(numeric, "int", false, true, zero);

        var boolean = Find("ctl00_PageContent_chkCustomerCodeForLogin");
        var disabled = new SettingChange(boolean.ParameterId, Json("true"), Json("false"), null);
        SqlPersistentSettingsTestService.ValidateCatalogChange(boolean, "bit", false, true, disabled);
        var projected = SettingUpdateValidator.ValidateAndProject(Catalog, new UpdateRequest([disabled]),
            new Dictionary<string, object?> { [boolean.ParameterId] = true }, true);
        Assert.Equal(false, projected[boolean.ParameterId]);
    }

    private static ParameterDefinition Find(string id) => Catalog.AllSettings.Single(setting => setting.ParameterId == id);

    private static JsonElement Json(string value) => JsonDocument.Parse(value).RootElement.Clone();
}
