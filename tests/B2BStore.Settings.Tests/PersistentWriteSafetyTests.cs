using System.Globalization;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class PersistentWriteSafetyTests
{
    private const string ConnectionVariable = "B2BSTORE_SQL_CONNECTION";
    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));

    [Fact]
    public async Task PersistentSaveRequiresAuthenticationAndExplicitTestMode()
    {
        var service = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", true);
        var request = new PersistentSettingsSaveRequest(new PersistentSiteStatusChange(1, 0), null);
        var auth = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(request, false, CancellationToken.None));
        Assert.Equal("TestWriteUnauthorized", auth.Code);

        var disabled = new SqlPersistentSettingsTestService(Catalog, AnyConnection(), "store5", false);
        var mode = await Assert.ThrowsAsync<SettingsException>(() => disabled.SaveAsync(request, true, CancellationToken.None));
        Assert.Equal("TestWriteModeRequired", mode.Code);
    }

    [Fact]
    public async Task PersistentSaveRejectsAnalysisDatabaseAndEveryOtherMemberBeforeConnecting()
    {
        var builder = new SqlConnectionStringBuilder(AnyConnection()) { InitialCatalog = "Store5_Analysis" };
        var wrongDatabase = new SqlPersistentSettingsTestService(Catalog, builder.ConnectionString, "store5", true);
        var databaseError = await Assert.ThrowsAsync<SettingsException>(() => wrongDatabase.SaveAsync(
            new PersistentSettingsSaveRequest(new PersistentSiteStatusChange(null, 1), null), true, CancellationToken.None));
        Assert.Equal("DatabaseIsolationViolation", databaseError.Code);

        var wrongMember = new SqlPersistentSettingsTestService(Catalog, AnyConnection(), "store6", true);
        var memberError = await Assert.ThrowsAsync<SettingsException>(() => wrongMember.SaveAsync(
            new PersistentSettingsSaveRequest(new PersistentSiteStatusChange(null, 1), null), true, CancellationToken.None));
        Assert.Equal("DatabaseIsolationViolation", memberError.Code);
    }

    [Fact]
    public async Task PersistentSaveRejectsInvalidOptionsAndBigintValuesBeforeConnecting()
    {
        var service = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", true);
        var status = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(new PersistentSiteStatusChange(1, 3), null), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", status.Code);

        var badFlag = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, new PersistentParameterOrderChange("0", [new BitOperation("3", true)])), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", badFlag.Code);

        var overflow = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, new PersistentParameterOrderChange("9223372036854775808", [new BitOperation("1", true)])), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", overflow.Code);

        var badLayout = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, new PersistentLayoutChange(1, 2)), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", badLayout.Code);

        var badMenu = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, null, new PersistentMenuVisibilityChange(1, 2)), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", badMenu.Code);

        var nullOriginal = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, new PersistentLayoutChange(null, 0)), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", nullOriginal.Code);
    }

    [Fact]
    public async Task CatalogSaveRequiresAuthenticationAndExplicitTestMode()
    {
        var request = CatalogRequest("ctl00_PageContent_ddlProductTableStockType", "0", "1");
        var service = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", true);
        var unauthorized = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(request, false, CancellationToken.None));
        Assert.Equal("TestWriteUnauthorized", unauthorized.Code);
        var disabled = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", false);
        var mode = await Assert.ThrowsAsync<SettingsException>(() => disabled.SaveAsync(request, true, CancellationToken.None));
        Assert.Equal("TestWriteModeRequired", mode.Code);
    }

    [Fact]
    public async Task CatalogSaveRejectsUnallowlistedFieldsAndInvalidScalarOrBitmaskValuesBeforeConnecting()
    {
        var service = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", true);
        var notAllowed = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            CatalogRequest("ctl00_PageContent_ddlProductSearchParameter", "1", "2"), true, CancellationToken.None));
        Assert.Equal("MappingNotVerified", notAllowed.Code);

        var invalidEnum = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            CatalogRequest("ctl00_PageContent_ddlProductTableStockType", "1", "99"), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", invalidEnum.Code);

        var invalidBit = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, Changes: [new SettingChange(
                "ctl00_PageContent_ddlParameterStock", JsonDocument.Parse("\"0\"").RootElement.Clone(), JsonDocument.Parse("null").RootElement.Clone(),
                [new BitOperation("3", true)])]), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", invalidBit.Code);

        var invalidBooleanDropdown = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, Changes: [new SettingChange(
                "ctl00_PageContent_ddlCurrencyType", JsonDocument.Parse("false").RootElement.Clone(),
                JsonDocument.Parse("\"2\"").RootElement.Clone(), null)]), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", invalidBooleanDropdown.Code);

        var invalidIntegerCheckbox = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, Changes: [new SettingChange(
                "ctl00_PageContent_chkDoNotSendMailToCustomer", JsonDocument.Parse("\"0\"").RootElement.Clone(),
                JsonDocument.Parse("\"true\"").RootElement.Clone(), null)]), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", invalidIntegerCheckbox.Code);

        var invalidTextType = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(null, null, Changes: [new SettingChange(
                "ctl00_PageContent_txtLayoutColor", JsonDocument.Parse("\"#ffffff\"").RootElement.Clone(),
                JsonDocument.Parse("true").RootElement.Clone(), null)]), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", invalidTextType.Code);
    }

    [Fact]
    public async Task CatalogSaveAcceptsOnlyInvariantSqlRangeValuesForVerifiedNumericTextAdapters()
    {
        var service = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", true);
        foreach (var (id, expected, value) in new[]
        {
            ("ctl00_PageContent_txtMinCartCount", "1", "2147483648"),
            ("ctl00_PageContent_txtMaximumOrderQuantity", "1", "1e2"),
            ("ctl00_PageContent_txtMinCartTotal", "1.00", "1,25"),
            ("ctl00_PageContent_txtMinCartTotal", "1.00", "1.234"),
            ("ctl00_PageContent_txtMinCartTotal", "1.00", "10000000000000000"),
        })
        {
            var error = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
                CatalogRequest(id, expected, value), true, CancellationToken.None));
            Assert.Equal("ValidationFailed", error.Code);
        }

        var parameter = SqlValueConverter.CreateParameter("@amount", "decimal", "12.34", 18, 2);
        Assert.Equal((byte)18, parameter.Precision);
        Assert.Equal((byte)2, parameter.Scale);
        Assert.Equal(12.34m, parameter.Value);
    }

    [Fact]
    public async Task CatalogSaveRejectsStore5AnalysisEvenWhenTestModeAndAuthenticationAreEnabled()
    {
        var builder = new SqlConnectionStringBuilder(TestDatabaseConnection()) { InitialCatalog = "Store5_Analysis" };
        var service = new SqlPersistentSettingsTestService(Catalog, builder.ConnectionString, "store5", true);
        var error = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            CatalogRequest("ctl00_PageContent_ddlProductTableStockType", "0", "1"), true, CancellationToken.None));
        Assert.Equal("DatabaseIsolationViolation", error.Code);
    }

    [Fact]
    public async Task CatalogSaveRejectsMixedTypedAndCatalogChangesBeforeConnecting()
    {
        var request = CatalogRequest("ctl00_PageContent_ddlProductTableStockType", "0", "1") with
        {
            SiteStatus = new PersistentSiteStatusChange(0, 1),
        };
        var service = new SqlPersistentSettingsTestService(Catalog, TestDatabaseConnection(), "store5", true);
        var error = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(request, true, CancellationToken.None));
        Assert.Equal("ValidationFailed", error.Code);
    }

    [Fact]
    public async Task CatalogSaveRejectsStaleExpectedValueWithoutChangingTheTargetRow()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable("B2BSTORE_SQL_WRITE_TEST_MODE"), "true", StringComparison.OrdinalIgnoreCase),
            "The local isolated SQL test flag is required for this integration check.");
        var catalog = Catalog;
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable)!;
        var parameterId = "ctl00_PageContent_ddlProductTableStockType";
        var before = await new SqlServerSettingsStore(catalog, connection, "store5").ReadAsync(CancellationToken.None);
        var current = before.Items.Single(item => item.ParameterId == parameterId).Value as string;
        Assert.False(string.IsNullOrEmpty(current));
        var definition = catalog.AllSettings.Single(setting => setting.ParameterId == parameterId);
        var staleExpected = definition.Options.First(option => option.Value != current).Value;
        var newValue = definition.Options.First(option => option.Value != current && option.Value != staleExpected).Value;
        var staleRequest = CatalogRequest(parameterId, staleExpected, newValue);
        var service = new SqlPersistentSettingsTestService(catalog, connection, "store5", true);
        var conflict = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(staleRequest, true, CancellationToken.None));
        Assert.Equal("ConcurrencyConflict", conflict.Code);

        var after = await new SqlServerSettingsStore(catalog, connection, "store5").ReadAsync(CancellationToken.None);
        Assert.True(SettingUpdateValidator.ValuesEqual(current, after.Items.Single(item => item.ParameterId == parameterId).Value),
            "A stale generic save modified the target field.");
    }

    [Fact]
    public void CatalogMarksCandidateTypesButDoesNotClaimLegacySaveVerification()
    {
        var theme = Catalog.AllSettings.Single(x => x.ParameterId == "ctl00_PageContent_ddParameterThema");
        var stockType = Catalog.AllSettings.Single(x => x.ParameterId == "ctl00_PageContent_ddlProductTableStockType");
        Assert.Equal("int", theme.Mapping.SqlDataType);
        Assert.Equal("int", stockType.Mapping.SqlDataType);
        Assert.Equal("5", stockType.Options[4].Value);
        Assert.Equal("4", stockType.Options[5].Value);
        Assert.Equal("candidate", stockType.Mapping.Status);
    }

    [Fact]
    public async Task PersistentSaveRejectsStaleExpectedValueWithoutChangingTheTargetRow()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable("B2BSTORE_SQL_WRITE_TEST_MODE"), "true", StringComparison.OrdinalIgnoreCase),
            "The local isolated SQL test flag is required for this integration check.");
        var catalog = Catalog;
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable)!;
        var before = await new SqlServerSettingsStore(catalog, connection, "store5").ReadAsync(CancellationToken.None);
        var current = before.Items.Single(x => x.ParameterId == "ctl00_PageContent_ddlSiteStatus").Value;
        var currentInt = current is null ? (int?)null : int.Parse(Convert.ToString(current, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        var impossibleExpected = currentInt switch { 0 => 1, 1 => 0, _ => 2 };
        var service = new SqlPersistentSettingsTestService(catalog, connection, "store5", true);

        var conflict = await Assert.ThrowsAsync<SettingsException>(() => service.SaveAsync(
            new PersistentSettingsSaveRequest(new PersistentSiteStatusChange(impossibleExpected, 1), null), true, CancellationToken.None));
        Assert.Equal("ConcurrencyConflict", conflict.Code);

        var after = await new SqlServerSettingsStore(catalog, connection, "store5").ReadAsync(CancellationToken.None);
        Assert.True(SettingUpdateValidator.ValuesEqual(current, after.Items.Single(x => x.ParameterId == "ctl00_PageContent_ddlSiteStatus").Value),
            "The stale SiteStatus request modified the target field.");
    }

    [Fact]
    public async Task PersistentSaveRollsBackEarlierUpdateWhenLaterFieldConflicts()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable("B2BSTORE_SQL_WRITE_TEST_MODE"), "true", StringComparison.OrdinalIgnoreCase),
            "The local isolated SQL test flag is required for this integration check.");
        var catalog = Catalog;
        var connection = Environment.GetEnvironmentVariable(ConnectionVariable)!;
        var before = await new SqlServerSettingsStore(catalog, connection, "store5").ReadAsync(CancellationToken.None);
        var siteItem = before.Items.Single(x => x.ParameterId == "ctl00_PageContent_ddlSiteStatus");
        var orderItem = before.Items.Single(x => x.ParameterId == "ctl00_PageContent_ddlOrderParameter");
        var originalSite = siteItem.Value is null ? (int?)null : int.Parse(Convert.ToString(siteItem.Value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
        var originalOrder = orderItem.Value is null ? null : Convert.ToString(orderItem.Value, CultureInfo.InvariantCulture);
        Assert.True(originalSite is 0 or 1);
        Assert.NotNull(originalOrder);
        var staleOrder = originalOrder == "0" ? "1" : "0";
        var request = new PersistentSettingsSaveRequest(
            new PersistentSiteStatusChange(originalSite, originalSite == 1 ? 0 : 1),
            new PersistentParameterOrderChange(staleOrder, [new BitOperation("1", true)]));

        var conflict = await Assert.ThrowsAsync<SettingsException>(() => new SqlPersistentSettingsTestService(catalog, connection, "store5", true)
            .SaveAsync(request, true, CancellationToken.None));
        Assert.Equal("ConcurrencyConflict", conflict.Code);

        var after = await new SqlServerSettingsStore(catalog, connection, "store5").ReadAsync(CancellationToken.None);
        Assert.True(SettingUpdateValidator.ValuesEqual(siteItem.Value, after.Items.Single(x => x.ParameterId == "ctl00_PageContent_ddlSiteStatus").Value),
            "A concurrency rollback changed the original SiteStatus value.");
        Assert.True(SettingUpdateValidator.ValuesEqual(orderItem.Value, after.Items.Single(x => x.ParameterId == "ctl00_PageContent_ddlOrderParameter").Value),
            "A concurrency rollback changed the original ParameterOrder value.");
    }

    private static string AnyConnection() => Environment.GetEnvironmentVariable(ConnectionVariable)
        ?? "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Store5_Analysis;Integrated Security=True;TrustServerCertificate=True";

    private static string TestDatabaseConnection()
    {
        var builder = new SqlConnectionStringBuilder(AnyConnection()) { InitialCatalog = "Store5_Settings_Test" };
        return builder.ConnectionString;
    }

    private static PersistentSettingsSaveRequest CatalogRequest(string parameterId, string expected, string value) =>
        new(null, null, Changes: [new SettingChange(parameterId,
            JsonDocument.Parse(JsonSerializer.Serialize(expected)).RootElement.Clone(),
            JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement.Clone(), null)]);
}
