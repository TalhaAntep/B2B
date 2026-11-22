using System.Globalization;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class CatalogPersistentIntegrationTests
{
    private const string TestFlagName = "B2BSTORE_SQL_WRITE_TEST_MODE";
    private const string ConnectionName = "B2BSTORE_SQL_CONNECTION";
    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));

    [Fact]
    public async Task PersistsAndRestoresProductCompanyNullThroughAuthenticatedApplicationApi()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase),
            "The explicit isolated SQL write-test flag is required.");
        var baseAddress = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(baseAddress));
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(connection));
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);

        const string id = "ctl00_PageContent_ddlCommonSystem";
        var store = new SqlServerSettingsStore(Catalog, connection, "store5");
        var baseline = await store.ReadAsync(CancellationToken.None);
        Assert.Equal(98, baseline.Items.Count);
        Assert.Null(baseline.Items.Single(item => item.ParameterId == id).Value);
        await CreateAndVerifyBackupAsync(connection!, "product-company-null-contract");

        using var client = new HttpClient { BaseAddress = new Uri(baseAddress!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var attempted = false;
        try
        {
            var change = new SettingChange(id, JsonNullValue(), JsonValue("1"), null);
            attempted = true;
            using var content = new StringContent(JsonSerializer.Serialize(new UpdateRequest([change]), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PutAsync("/api/settings", content);
            Assert.True(response.IsSuccessStatusCode, $"The authenticated API rejected the ProductCompany update with HTTP {(int)response.StatusCode}.");
            var result = await response.Content.ReadFromJsonAsync<PersistentSettingsSaveResult>();
            Assert.True(result?.Committed == true);
            Assert.Equal("1", Convert.ToString(result!.Values![id], CultureInfo.InvariantCulture));
            var apiRead = await client.GetFromJsonAsync<SettingsResponse>("/api/settings");
            Assert.Equal("1", Convert.ToString(apiRead!.Items.Single(item => item.ParameterId == id).Value, CultureInfo.InvariantCulture));
            var sqlRead = await store.ReadAsync(CancellationToken.None);
            Assert.Equal("1", Convert.ToString(sqlRead.Items.Single(item => item.ParameterId == id).Value, CultureInfo.InvariantCulture));
        }
        finally
        {
            if (attempted)
            {
                var current = await store.ReadAsync(CancellationToken.None);
                var value = current.Items.Single(item => item.ParameterId == id).Value;
                if (value is not null)
                {
                    var restore = new SettingChange(id, JsonValue(value), JsonNullValue(), null);
                    using var restoreContent = new StringContent(JsonSerializer.Serialize(new UpdateRequest([restore]), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                        System.Text.Encoding.UTF8, "application/json");
                    using var restoreResponse = await client.PutAsync("/api/settings", restoreContent);
                    Assert.True(restoreResponse.IsSuccessStatusCode, "ProductCompany restoration failed; stop all further writes.");
                }
                var apiRestored = await client.GetFromJsonAsync<SettingsResponse>("/api/settings");
                var sqlRestored = await store.ReadAsync(CancellationToken.None);
                Assert.Null(apiRestored!.Items.Single(item => item.ParameterId == id).Value);
                Assert.Null(sqlRestored.Items.Single(item => item.ParameterId == id).Value);
            }
        }
    }

    [Fact]
    public async Task PersistsScheduleAsSqlScalarAndRestoresOriginalValueThroughAuthenticatedApi()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase),
            "The explicit isolated SQL write-test flag is required.");
        var baseAddress = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(baseAddress));
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(connection));
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);

        const string id = "ctl00_PageContent_ddlSchedule";
        var store = new SqlServerSettingsStore(Catalog, connection, "store5");
        var baseline = await store.ReadAsync(CancellationToken.None);
        Assert.Equal(98, baseline.Items.Count);
        var original = baseline.Items.Single(item => item.ParameterId == id).Value;
        Assert.True(original is null or "0" or "1", "Schedule is outside the consumer-confirmed scalar range.");
        await CreateAndVerifyBackupAsync(connection!, "schedule-scalar-serializer");

        using var client = new HttpClient { BaseAddress = new Uri(baseAddress!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var attempted = false;
        try
        {
            var next = original as string == "1" ? "0" : "1";
            var change = new SettingChange(id, JsonValue(original), JsonNullValue(), [new BitOperation("1", next == "1")]);
            attempted = true;
            using var content = new StringContent(JsonSerializer.Serialize(new UpdateRequest([change]), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PutAsync("/api/settings", content);
            Assert.True(response.IsSuccessStatusCode, $"The authenticated API rejected Schedule with HTTP {(int)response.StatusCode}.");
            var result = await response.Content.ReadFromJsonAsync<PersistentSettingsSaveResult>();
            Assert.True(result?.Committed == true);
            Assert.Equal(next, Convert.ToString(result!.Values![id], CultureInfo.InvariantCulture));
            var apiRead = await client.GetFromJsonAsync<SettingsResponse>("/api/settings");
            Assert.Equal(next, Convert.ToString(apiRead!.Items.Single(item => item.ParameterId == id).Value, CultureInfo.InvariantCulture));
            var sqlRead = await store.ReadAsync(CancellationToken.None);
            Assert.Equal(next, Convert.ToString(sqlRead.Items.Single(item => item.ParameterId == id).Value, CultureInfo.InvariantCulture));
        }
        finally
        {
            if (attempted)
            {
                var current = await store.ReadAsync(CancellationToken.None);
                var currentValue = current.Items.Single(item => item.ParameterId == id).Value;
                if (!SettingUpdateValidator.ValuesEqual(original, currentValue))
                {
                    BitOperation[] operations = original is null ? []
                        : [new BitOperation("1", Convert.ToString(original, CultureInfo.InvariantCulture) == "1")];
                    var restore = new SettingChange(id, JsonValue(currentValue), JsonNullValue(), operations);
                    using var restoreContent = new StringContent(JsonSerializer.Serialize(new UpdateRequest([restore]), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                        System.Text.Encoding.UTF8, "application/json");
                    using var restoreResponse = await client.PutAsync("/api/settings", restoreContent);
                    Assert.True(restoreResponse.IsSuccessStatusCode, "Schedule restoration failed; stop all further writes.");
                }
                var apiRestored = await client.GetFromJsonAsync<SettingsResponse>("/api/settings");
                var sqlRestored = await store.ReadAsync(CancellationToken.None);
                Assert.True(SettingUpdateValidator.ValuesEqual(original, apiRestored!.Items.Single(item => item.ParameterId == id).Value));
                Assert.True(SettingUpdateValidator.ValuesEqual(original, sqlRestored.Items.Single(item => item.ParameterId == id).Value));
            }
        }
    }

    [Fact]
    public async Task PersistsAndRestoresAllAdditionalFieldsInBackupVerifiedGroups()
    {
        await VerifyGroupAsync("baseline-five", [
            "ctl00_PageContent_ddlSiteStatus",
            "ctl00_PageContent_ddlOrderParameter",
            "ctl00_PageContent_ddlLayout",
            "ctl00_PageContent_ddlDontShowMenuAtDealers",
            "ctl00_PageContent_chkDeliveryDateShow",
        ]);
        await VerifyGroupAsync("scalar-theme-stock-design", [
            "ctl00_PageContent_ddParameterThema",
            "ctl00_PageContent_ddlProductTableStockType",
            "ctl00_PageContent_ddlParameterDesign",
        ]);
        await VerifyGroupAsync("product-detail-stock", [
            "ctl00_PageContent_ddlParameterStock",
            "ctl00_PageContent_ddlProductDetailParameter",
            "ctl00_PageContent_ddlProductDetailParameter2",
            "ctl00_PageContent_ddlParameterProductDetailNotLogin",
        ]);
        await VerifyGroupAsync("account-menu-search", [
            "ctl00_PageContent_ddlExportProductList",
            "ctl00_PageContent_ddlParameterAccount",
            "ctl00_PageContent_ddMainProducts",
            "ctl00_PageContent_ddlParameterMenu",
            "ctl00_PageContent_ddlParameterDynamicSearch",
        ]);
        await VerifyGroupAsync("scalar-display-controls", [
            "ctl00_PageContent_ddlCategoryMenu",
            "ctl00_PageContent_ddlFilterSort",
            "ctl00_PageContent_ddlProductSort",
            "ctl00_PageContent_ddlCariLimitTracing",
            "ctl00_PageContent_ddlCariLimitTracingInfo",
            "ctl00_PageContent_ddlDontShowPrice",
            "ctl00_PageContent_ddlTaxInRatio",
            "ctl00_PageContent_ddlCartProductVariationCodeShow",
            "ctl00_PageContent_ddlHomeStyle",
            "ctl00_PageContent_ddlSearchMarka",
            "ctl00_PageContent_ddlSearchDetail",
            "ctl00_PageContent_ddlRowDescriptionShow",
            "ctl00_PageContent_ddlAllProductsShow",
            "ctl00_PageContent_DDLDeliveryAddress",
            "ctl00_PageContent_ddlSiteMap",
            "ctl00_PageContent_ddSubDealerShow",
        ]);
        await VerifyGroupAsync("boolean-controls", [
            "ctl00_PageContent_chkCustomerCodeForLogin",
            "ctl00_PageContent_chkLoginWithExpiry",
            "ctl00_PageContent_chkDontUseLcnAutoComplate",
            "ctl00_PageContent_chkAddCartAction",
            "ctl00_PageContent_chkBillingAdress",
        ]);
    }

    [Fact]
    public async Task PersistsAndRestoresNewTextScalarFields()
    {
        await VerifyGroupAsync("new-text-scalars", [
            "ctl00_PageContent_txtProductImageAspectRatio", "ctl00_PageContent_txtLayoutColor", "ctl00_PageContent_txtLayoutColor2",
            "ctl00_PageContent_txtGoogleAnalysticScript", "ctl00_PageContent_txtGoogleAnalysticProfileID",
            "ctl00_PageContent_txtGoogleAdWordsReMarketingCode", "ctl00_PageContent_txtMinStockFilter",
            "ctl00_PageContent_txtRegisterFormUrl", "ctl00_PageContent_txtBayiTelefon", "ctl00_PageContent_txtBayiAdres",
            "ctl00_PageContent_txtPaymentLink", "ctl00_PageContent_TxtDealerName", "ctl00_PageContent_txtBayiYetkiliKisi",
            "ctl00_PageContent_txtBayiWebAdres", "ctl00_PageContent_txtEInvoiceNo",
        ]);
    }

    [Fact]
    public async Task PersistsAndRestoresNewScalarSelectFields()
    {
        await VerifyGroupAsync("new-scalar-selects", [
            "ctl00_PageContent_ddlHasStock", "ctl00_PageContent_ddlPaymentDetailShow", "ctl00_PageContent_ddlBankTableType",
            "ctl00_PageContent_ddlCurrencyType", "ctl00_PageContent_ddlExpiryDate",
            "ctl00_PageContent_ddlAccountActivityGenralCuurencyTableShow", "ctl00_PageContent_ddlCommissionType",
            "ctl00_PageContent_ddlSpecialDiscount", "ctl00_PageContent_ddlPaymentForAccountActivityShow",
            "ctl00_PageContent_ddlUseDesktopApi", "ctl00_PageContent_ddlProductCurrency", "ctl00_PageContent_ddlPosCurrency",
            "ctl00_PageContent_ddlSendDeliveryAddressIfDifferent", "ctl00_PageContent_ddlCurrencyPriceType",
            "ctl00_PageContent_ddlintegrated", "ctl00_PageContent_ddldiscountTypeInvoice", "ctl00_PageContent_ddlBonusShow",
            "ctl00_PageContent_ddlUseMd5Encryption", "ctl00_PageContent_ddlVariationShow", "ctl00_PageContent_ddIntegrationCurrency",
        ]);
    }

    [Fact]
    public async Task PersistsAndRestoresNewIntegerBooleanCheckbox()
    {
        await VerifyGroupAsync("new-integer-checkbox", ["ctl00_PageContent_chkDoNotSendMailToCustomer"]);
    }

    [Fact]
    public async Task RestoresIntegerCheckboxAfterPreviousRestorationBug()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase),
            "The explicit isolated SQL write-test flag is required.");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(connection), "The isolated SQL test connection is required.");
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);
        var catalog = Catalog;
        const string id = "ctl00_PageContent_chkDoNotSendMailToCustomer";
        var store = new SqlServerSettingsStore(catalog, connection, "store5");
        var current = await store.ReadAsync(CancellationToken.None);
        var currentText = Convert.ToString(current.Items.Single(item => item.ParameterId == id).Value, CultureInfo.InvariantCulture);
        Assert.Contains(currentText, new[] { "0", "1" });
        var originalText = currentText == "1" ? "0" : "1";
        await CreateAndVerifyBackupAsync(connection!, "integer-checkbox-recovery");
        var service = new SqlPersistentSettingsTestService(catalog, connection, "store5", true);
        var restoration = new SettingChange(id, JsonValue(currentText), JsonValue(originalText == "1"), null);
        var result = await service.SaveAsync(new PersistentSettingsSaveRequest(null, null, Changes: [restoration]), true, CancellationToken.None);
        Assert.True(result.Committed, "The interrupted isolated setting was not restored.");
        var restored = await store.ReadAsync(CancellationToken.None);
        Assert.Equal(originalText, Convert.ToString(restored.Items.Single(item => item.ParameterId == id).Value, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task PersistsAndRestoresNewSafeBitmaskFields()
    {
        await VerifyGroupAsync("new-safe-bitmasks", [
            "ctl00_PageContent_ddlParameterLogin", "ctl00_PageContent_ddlParameterLoginMenu",
            "ctl00_PageContent_ddlParameterAdmin", "ctl00_PageContent_ddlParameterIntegration",
        ]);
    }

    [Fact]
    public async Task PersistsAndRestoresNewFieldsThroughAuthenticatedApplicationApi()
    {
        var baseAddress = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        Assert.False(string.IsNullOrWhiteSpace(baseAddress), "The local isolated test API URL is required.");
        Assert.False(string.IsNullOrWhiteSpace(token), "The local administrator token must be supplied through the test process environment.");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(connection), "The isolated SQL test connection is required.");
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);
        using var client = new HttpClient { BaseAddress = new Uri(baseAddress!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var definitions = Catalog.AllSettings.ToDictionary(setting => setting.ParameterId, StringComparer.Ordinal);
        var groups = new (string Label, string[] Ids)[]
        {
            ("api-text-scalars", ["ctl00_PageContent_txtProductImageAspectRatio", "ctl00_PageContent_txtLayoutColor", "ctl00_PageContent_txtLayoutColor2",
                "ctl00_PageContent_txtGoogleAnalysticScript", "ctl00_PageContent_txtGoogleAnalysticProfileID", "ctl00_PageContent_txtGoogleAdWordsReMarketingCode",
                "ctl00_PageContent_txtMinStockFilter", "ctl00_PageContent_txtRegisterFormUrl", "ctl00_PageContent_txtBayiTelefon", "ctl00_PageContent_txtBayiAdres",
                "ctl00_PageContent_txtPaymentLink", "ctl00_PageContent_TxtDealerName", "ctl00_PageContent_txtBayiYetkiliKisi", "ctl00_PageContent_txtBayiWebAdres", "ctl00_PageContent_txtEInvoiceNo"]),
            ("api-scalar-selects", ["ctl00_PageContent_ddlHasStock", "ctl00_PageContent_ddlPaymentDetailShow", "ctl00_PageContent_ddlBankTableType",
                "ctl00_PageContent_ddlCurrencyType", "ctl00_PageContent_ddlExpiryDate", "ctl00_PageContent_ddlAccountActivityGenralCuurencyTableShow",
                "ctl00_PageContent_ddlCommissionType", "ctl00_PageContent_ddlSpecialDiscount", "ctl00_PageContent_ddlPaymentForAccountActivityShow",
                "ctl00_PageContent_ddlUseDesktopApi", "ctl00_PageContent_ddlProductCurrency", "ctl00_PageContent_ddlPosCurrency",
                "ctl00_PageContent_ddlSendDeliveryAddressIfDifferent", "ctl00_PageContent_ddlCurrencyPriceType", "ctl00_PageContent_ddlintegrated",
                "ctl00_PageContent_ddldiscountTypeInvoice", "ctl00_PageContent_ddlBonusShow", "ctl00_PageContent_ddlUseMd5Encryption",
                "ctl00_PageContent_ddlVariationShow", "ctl00_PageContent_ddIntegrationCurrency"]),
            ("api-integer-checkbox", ["ctl00_PageContent_chkDoNotSendMailToCustomer"]),
            ("api-safe-bitmasks", ["ctl00_PageContent_ddlParameterLogin", "ctl00_PageContent_ddlParameterLoginMenu",
                "ctl00_PageContent_ddlParameterAdmin", "ctl00_PageContent_ddlParameterIntegration"]),
        };

        foreach (var (label, ids) in groups)
            await VerifyApiGroupAsync(client, connection!, label, ids, definitions);
    }

    [Fact]
    public async Task PersistsAndRestoresPreviouslyServiceOnlyFieldsThroughAuthenticatedApplicationApi()
    {
        var baseAddress = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(baseAddress), "The local isolated test API URL is required.");
        Assert.False(string.IsNullOrWhiteSpace(token), "The local administrator token must be supplied through the test process environment.");
        Assert.False(string.IsNullOrWhiteSpace(connection), "The isolated SQL test connection is required.");
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);

        using var client = new HttpClient { BaseAddress = new Uri(baseAddress!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var definitions = Catalog.AllSettings.ToDictionary(setting => setting.ParameterId, StringComparer.Ordinal);
        await VerifyApiGroupAsync(client, connection!, "previously-service-only-api", [
            "ctl00_PageContent_ddlLayout",
            "ctl00_PageContent_ddlDontShowMenuAtDealers",
            "ctl00_PageContent_chkDeliveryDateShow",
            "ctl00_PageContent_ddParameterThema",
            "ctl00_PageContent_ddlProductTableStockType",
            "ctl00_PageContent_ddlParameterDesign",
            "ctl00_PageContent_ddlParameterStock",
            "ctl00_PageContent_ddlProductDetailParameter",
            "ctl00_PageContent_ddlProductDetailParameter2",
            "ctl00_PageContent_ddlParameterProductDetailNotLogin",
            "ctl00_PageContent_ddlExportProductList",
            "ctl00_PageContent_ddlParameterAccount",
            "ctl00_PageContent_ddMainProducts",
            "ctl00_PageContent_ddlParameterMenu",
            "ctl00_PageContent_ddlParameterDynamicSearch",
            "ctl00_PageContent_ddlCategoryMenu",
            "ctl00_PageContent_ddlFilterSort",
            "ctl00_PageContent_ddlProductSort",
            "ctl00_PageContent_ddlCariLimitTracing",
            "ctl00_PageContent_ddlCariLimitTracingInfo",
            "ctl00_PageContent_ddlDontShowPrice",
            "ctl00_PageContent_ddlTaxInRatio",
            "ctl00_PageContent_ddlCartProductVariationCodeShow",
            "ctl00_PageContent_ddlHomeStyle",
            "ctl00_PageContent_ddlSearchMarka",
            "ctl00_PageContent_ddlSearchDetail",
            "ctl00_PageContent_ddlRowDescriptionShow",
            "ctl00_PageContent_ddlAllProductsShow",
            "ctl00_PageContent_DDLDeliveryAddress",
            "ctl00_PageContent_ddlSiteMap",
            "ctl00_PageContent_ddSubDealerShow",
            "ctl00_PageContent_chkCustomerCodeForLogin",
            "ctl00_PageContent_chkLoginWithExpiry",
            "ctl00_PageContent_chkDontUseLcnAutoComplate",
            "ctl00_PageContent_chkAddCartAction",
            "ctl00_PageContent_chkBillingAdress",
        ], definitions);
    }

    [Fact]
    public async Task PersistsAndRestoresFiveRemainingFieldsThroughAuthenticatedApplicationApiOneAtATime()
    {
        var baseAddress = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(baseAddress), "The local isolated test API URL is required.");
        Assert.False(string.IsNullOrWhiteSpace(token), "The local administrator token must be supplied through the test process environment.");
        Assert.False(string.IsNullOrWhiteSpace(connection), "The isolated SQL test connection is required.");
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);

        using var client = new HttpClient { BaseAddress = new Uri(baseAddress!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var definitions = Catalog.AllSettings.ToDictionary(setting => setting.ParameterId, StringComparer.Ordinal);
        var fields = new[]
        {
            "ctl00_PageContent_ddlOrderParameter",
            "ctl00_PageContent_ddlSiteStatus",
            "ctl00_PageContent_txtMaximumOrderQuantity",
            "ctl00_PageContent_txtMinCartCount",
            "ctl00_PageContent_txtMinCartTotal",
        };

        foreach (var id in fields)
            await VerifyApiGroupAsync(client, connection!, $"http-single-{id[(id.LastIndexOf('_') + 1)..]}", [id], definitions);
    }

    [Fact]
    public async Task PersistsAndRestoresUseFilteringDecimalTextMaskThroughAuthenticatedApplicationApi()
    {
        var setup = CreateAuthenticatedTestClient();
        using (setup.Client)
            await VerifyApiGroupAsync(setup.Client, setup.Connection, "use-filtering-decimal-text-mask",
                [DealerSettingValueCodec.UseFilteringParameterId], setup.Definitions);
    }

    [Fact]
    public async Task PersistsAndRestoresDiscountTypeSourceCodeThroughAuthenticatedApplicationApi()
    {
        var setup = CreateAuthenticatedTestClient();
        using (setup.Client)
            await VerifyApiGroupAsync(setup.Client, setup.Connection, "discount-type-source-code",
                [DealerSettingValueCodec.DiscountTypeParameterId], setup.Definitions);
    }

    private static (HttpClient Client, string Connection, IReadOnlyDictionary<string, ParameterDefinition> Definitions) CreateAuthenticatedTestClient()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase),
            "The explicit isolated SQL write-test flag is required.");
        var baseAddress = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(baseAddress));
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.False(string.IsNullOrWhiteSpace(connection));
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);
        var client = new HttpClient { BaseAddress = new Uri(baseAddress!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, connection!, Catalog.AllSettings.ToDictionary(setting => setting.ParameterId, StringComparer.Ordinal));
    }

    private static async Task VerifyApiGroupAsync(HttpClient client, string connection, string label,
        IReadOnlyList<string> ids, IReadOnlyDictionary<string, ParameterDefinition> definitions)
    {
        var beforeResponse = await client.GetAsync("/api/settings");
        Assert.True(beforeResponse.IsSuccessStatusCode, "Authenticated test API read failed.");
        var beforeBody = await beforeResponse.Content.ReadFromJsonAsync<SettingsResponse>();
        Assert.NotNull(beforeBody);
        Assert.Equal(98, beforeBody.Items.Count);
        Dictionary<string, object?> before = ids.ToDictionary(id => id,
            id => NormalizeJsonValue(beforeBody.Items.Single(item => item.ParameterId == id).Value), StringComparer.Ordinal);
        await CreateAndVerifyBackupAsync(connection, label);
        var proposed = BuildChanges(ids, before, definitions);
        var store = new SqlServerSettingsStore(Catalog, connection, "store5");
        var writeAttempted = false;
        try
        {
            foreach (var change in proposed.Changes)
            {
                writeAttempted = true;
                var requestJson = JsonSerializer.Serialize(new UpdateRequest([change]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
                using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
                using var response = await client.PutAsync("/api/settings", content);
                Assert.True(response.IsSuccessStatusCode, $"The authenticated application API rejected {change.ParameterId} with HTTP {(int)response.StatusCode}.");
                var saved = await response.Content.ReadFromJsonAsync<PersistentSettingsSaveResult>();
                Assert.True(saved?.Committed == true, $"The application API did not confirm {change.ParameterId}.");
                Assert.True(SettingUpdateValidator.ValuesEqual(NormalizeJsonValue(saved.Values![change.ParameterId]), proposed.ExpectedValues[change.ParameterId]),
                    "The application API response did not contain the verified field value.");

                var apiRead = await client.GetAsync("/api/settings");
                Assert.True(apiRead.IsSuccessStatusCode, "The authenticated API read-after-write failed.");
                var apiBody = await apiRead.Content.ReadFromJsonAsync<SettingsResponse>();
                Assert.True(SettingUpdateValidator.ValuesEqual(NormalizeJsonValue(apiBody!.Items.Single(item => item.ParameterId == change.ParameterId).Value),
                    proposed.ExpectedValues[change.ParameterId]), "The API read-after-write value did not match.");
                var sqlRead = await store.ReadAsync(CancellationToken.None);
                Assert.True(SettingUpdateValidator.ValuesEqual(sqlRead.Items.Single(item => item.ParameterId == change.ParameterId).Value,
                    proposed.ExpectedValues[change.ParameterId]), "The direct SQL read-after-write value did not match.");
                if (change.ParameterId is DealerSettingValueCodec.UseFilteringParameterId or DealerSettingValueCodec.DiscountTypeParameterId)
                {
                    var rawValue = await ReadRawSupportedValueAsync(connection, change.ParameterId);
                    var expectedRaw = DealerSettingValueCodec.ToDatabaseValue(change.ParameterId, proposed.ExpectedValues[change.ParameterId]);
                    Assert.True(SettingUpdateValidator.ValuesEqual(rawValue, expectedRaw),
                        "The direct SQL column did not contain the field-specific stored representation.");
                }
                if (definitions[change.ParameterId].ControlType == "bitmask")
                    AssertBitmaskPreserved(before[change.ParameterId], proposed.ExpectedValues[change.ParameterId], definitions[change.ParameterId]);
            }
        }
        finally
        {
            if (writeAttempted)
                await RestoreViaApiAndVerifyAsync(client, ids, before, definitions, connection, store);
        }
    }

    private static async Task RestoreViaApiAndVerifyAsync(HttpClient client, IReadOnlyList<string> ids,
        IReadOnlyDictionary<string, object?> original, IReadOnlyDictionary<string, ParameterDefinition> definitions,
        string connection, SqlServerSettingsStore store)
    {
        var current = await store.ReadAsync(CancellationToken.None);
        var currentValues = ids.ToDictionary(id => id, id => current.Items.Single(item => item.ParameterId == id).Value, StringComparer.Ordinal);
        var restoreChanges = new List<SettingChange>();
        foreach (var id in ids)
        {
            if (SettingUpdateValidator.ValuesEqual(original[id], currentValues[id])) continue;
            if (original[id] is null || currentValues[id] is null)
                throw new InvalidOperationException("A test field became NULL; exact restoration cannot be constructed safely.");
            if (definitions[id].ControlType == "bitmask")
            {
                var originalMask = ParseMask(original[id]!);
                var currentMask = ParseMask(currentValues[id]!);
                var operations = definitions[id].Options.Select(option => option.Value).Distinct(StringComparer.Ordinal)
                    .Where(value => IsSet(originalMask, value) != IsSet(currentMask, value))
                    .Select(value => new BitOperation(value, IsSet(originalMask, value))).ToArray();
                if (BitmaskEditor.Apply(currentMask, operations, definitions[id].Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal)) != originalMask)
                    throw new InvalidOperationException("An unrelated or unknown bit changed; restoration stopped.");
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValues[id]), JsonNullValue(), operations));
            }
            else if (IsBitDropdown(id))
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValues[id]), JsonValue((bool)original[id]! ? "1" : "0"), null));
            else if (IsIntegerCheckbox(id))
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValues[id]), JsonValue(Convert.ToString(original[id], CultureInfo.InvariantCulture) == "1"), null));
            else
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValues[id]), JsonValue(original[id]), null));
        }

        foreach (var change in restoreChanges)
        {
            var requestJson = JsonSerializer.Serialize(new UpdateRequest([change]), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            using var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PutAsync("/api/settings", content);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"API restoration failed for {change.ParameterId}; no further writes may proceed.");
        }

        var apiResponse = await client.GetAsync("/api/settings");
        if (!apiResponse.IsSuccessStatusCode) throw new InvalidOperationException("API read-back after restoration failed.");
        var restoredApi = await apiResponse.Content.ReadFromJsonAsync<SettingsResponse>();
        var restoredSql = await store.ReadAsync(CancellationToken.None);
        foreach (var id in ids)
        {
            var apiValue = NormalizeJsonValue(restoredApi!.Items.Single(item => item.ParameterId == id).Value);
            var sqlValue = restoredSql.Items.Single(item => item.ParameterId == id).Value;
            if (!SettingUpdateValidator.ValuesEqual(apiValue, original[id]) || !SettingUpdateValidator.ValuesEqual(sqlValue, original[id]))
                throw new InvalidOperationException($"Original-value restoration could not be confirmed for {id}; no further writes may proceed.");
            if (id is DealerSettingValueCodec.UseFilteringParameterId or DealerSettingValueCodec.DiscountTypeParameterId)
            {
                var rawValue = await ReadRawSupportedValueAsync(connection, id);
                var expectedRaw = DealerSettingValueCodec.ToDatabaseValue(id, original[id]);
                if (!SettingUpdateValidator.ValuesEqual(rawValue, expectedRaw))
                    throw new InvalidOperationException($"Exact raw SQL restoration could not be confirmed for {id}; no further writes may proceed.");
            }
        }
    }

    private static async Task<object?> ReadRawSupportedValueAsync(string connectionString, string parameterId)
    {
        var column = parameterId switch
        {
            DealerSettingValueCodec.UseFilteringParameterId => "UseFiltering",
            DealerSettingValueCodec.DiscountTypeParameterId => "DiscountType",
            _ => throw new InvalidOperationException("Only fixed field-specific columns are supported by this assertion."),
        };
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using var command = new SqlCommand($"SELECT COUNT_BIG(*),MIN([{column}]) FROM dbo.DealerSettings WHERE MemberCode=@memberCode;", connection);
        command.Parameters.AddWithValue("@memberCode", "store5");
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        Assert.True(await reader.ReadAsync(CancellationToken.None));
        RowSelectionGuard.EnsureSingleTarget(Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture));
        return reader.IsDBNull(1) ? null : reader.GetValue(1);
    }

    private static void AssertBitmaskPreserved(object? originalValue, object? expectedValue, ParameterDefinition definition)
    {
        var originalMask = ParseMask(originalValue!);
        var expectedMask = ParseMask(expectedValue!);
        var knownMask = definition.Options.Select(option => option.Value).Distinct(StringComparer.Ordinal)
            .Aggregate(0L, (mask, value) => mask | long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture));
        Assert.Equal(originalMask & ~knownMask, expectedMask & ~knownMask);
        var changedOption = definition.Options[0].Value;
        foreach (var option in definition.Options.Where(option => option.Value != changedOption))
            Assert.Equal(IsSet(originalMask, option.Value), IsSet(expectedMask, option.Value));
    }

    private static async Task VerifyGroupAsync(string backupLabel, IReadOnlyList<string> parameterIds)
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase),
            "The explicit isolated SQL write-test flag is required.");
        var connection = Environment.GetEnvironmentVariable(ConnectionName);
        Assert.False(string.IsNullOrWhiteSpace(connection), "The isolated SQL test connection is required.");
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);

        var catalog = Catalog;
        var definitions = catalog.AllSettings.ToDictionary(setting => setting.ParameterId, StringComparer.Ordinal);
        var service = new SqlPersistentSettingsTestService(catalog, connection, "store5", true);
        var store = new SqlServerSettingsStore(catalog, connection, "store5");
        var original = await store.ReadAsync(CancellationToken.None);
        var before = parameterIds.ToDictionary(id => id,
            id => original.Items.Single(item => item.ParameterId == id).Value, StringComparer.Ordinal);
        foreach (var id in parameterIds.Where(IsBitDropdown))
            await AssertSqlBaselineMatchesAsync(connection!, CatalogWriteColumn(id), before[id]);
        await CreateAndVerifyBackupAsync(connection!, backupLabel);
        var proposed = BuildChanges(parameterIds, before, definitions);
        var wrote = false;

        try
        {
            wrote = true;
            foreach (var change in proposed.Changes)
            {
                var id = change.ParameterId;
                PersistentSettingsSaveResult result;
                try
                {
                    result = await service.SaveAsync(new PersistentSettingsSaveRequest(null, null, Changes: [change]), true, CancellationToken.None);
                }
                catch (SettingsException exception)
                {
                    throw new InvalidOperationException($"Isolated persistence failed for {id} ({exception.Code}).", exception);
                }
                Assert.True(result.Committed, "The isolated catalog save was not confirmed.");
                Assert.True(SettingUpdateValidator.ValuesEqual(result.Values![id], proposed.ExpectedValues[id]),
                    "A catalog value did not match the expected isolated test value.");
                var definition = definitions[id];
                if (definition.ControlType == "bitmask")
                {
                    var originalMask = ParseMask(before[id]!);
                    var testMask = ParseMask(proposed.ExpectedValues[id]!);
                    var knownMask = definition.Options.Select(option => option.Value).Distinct(StringComparer.Ordinal)
                        .Aggregate(0L, (mask, value) => mask | long.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture));
                    Assert.True((originalMask & ~knownMask) == (testMask & ~knownMask),
                        "A bit outside the inventoried option set changed during persistence.");
                    foreach (var optionValue in definition.Options.Select(option => option.Value).Distinct(StringComparer.Ordinal)
                        .Where(value => value != definition.Options[0].Value))
                        Assert.True(IsSet(originalMask, optionValue) == IsSet(testMask, optionValue),
                            "An unrelated known bit changed during persistence.");
                }

                var readBack = await store.ReadAsync(CancellationToken.None);
                Assert.True(SettingUpdateValidator.ValuesEqual(readBack.Items.Single(item => item.ParameterId == id).Value, proposed.ExpectedValues[id]),
                    "The isolated SQL read-after-write value did not match.");
            }
        }
        finally
        {
            if (wrote)
                await RestoreAndVerifyAsync(parameterIds, before, definitions, store, service);
        }
    }

    private static async Task CreateAndVerifyBackupAsync(string connectionString, string label)
    {
        var backupDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "B2BStore", "backups");
        Directory.CreateDirectory(backupDirectory);
        var backupPath = Path.GetFullPath(Path.Combine(backupDirectory,
            $"Store5_Settings_Test_{label}_{DateTime.UtcNow:yyyyMMdd-HHmmss-ffff}.bak"));
        var relative = Path.GetRelativePath(Path.GetFullPath(Environment.CurrentDirectory), backupPath);
        if (!relative.StartsWith("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Isolated SQL backups must remain outside the repository.");
        var escapedPath = backupPath.Replace("'", "''", StringComparison.Ordinal);
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using (var backup = new SqlCommand(
            $"BACKUP DATABASE [Store5_Settings_Test] TO DISK=N'{escapedPath}' WITH COPY_ONLY,CHECKSUM,INIT;", connection))
        {
            backup.CommandTimeout = 180;
            await backup.ExecuteNonQueryAsync(CancellationToken.None);
        }
        await using (var verify = new SqlCommand($"RESTORE VERIFYONLY FROM DISK=N'{escapedPath}' WITH CHECKSUM;", connection))
        {
            verify.CommandTimeout = 180;
            await verify.ExecuteNonQueryAsync(CancellationToken.None);
        }
        await using var header = new SqlCommand($"RESTORE HEADERONLY FROM DISK=N'{escapedPath}';", connection);
        await using var reader = await header.ExecuteReaderAsync(CancellationToken.None);
        var databaseNameOrdinal = reader.GetOrdinal("DatabaseName");
        if (!await reader.ReadAsync(CancellationToken.None)
            || !string.Equals(reader.GetString(databaseNameOrdinal), "Store5_Settings_Test", StringComparison.Ordinal))
            throw new InvalidOperationException("The pre-test backup header did not identify the approved database.");
    }

    private static async Task RestoreAndVerifyAsync(
        IReadOnlyList<string> parameterIds,
        IReadOnlyDictionary<string, object?> originalValues,
        IReadOnlyDictionary<string, ParameterDefinition> definitions,
        SqlServerSettingsStore store,
        SqlPersistentSettingsTestService service)
    {
        var current = await store.ReadAsync(CancellationToken.None);
        var currentValues = parameterIds.ToDictionary(id => id,
            id => current.Items.Single(item => item.ParameterId == id).Value, StringComparer.Ordinal);
        var restoreChanges = new List<SettingChange>();

        foreach (var id in parameterIds)
        {
            var definition = definitions[id];
            var originalValue = originalValues[id];
            var currentValue = currentValues[id];
            if (SettingUpdateValidator.ValuesEqual(originalValue, currentValue)) continue;
            if (originalValue is null || currentValue is null)
                throw new InvalidOperationException("A test field became NULL; exact restoration cannot be constructed safely.");

            if (definition.ControlType == "bitmask")
            {
                var originalMask = ParseMask(originalValue);
                var currentMask = ParseMask(currentValue);
                var operations = definition.Options
                    .Select(option => option.Value)
                    .Distinct(StringComparer.Ordinal)
                    .Where(value => IsSet(originalMask, value) != IsSet(currentMask, value))
                    .Select(value => new BitOperation(value, IsSet(originalMask, value)))
                    .ToArray();
                var restoredProjection = BitmaskEditor.Apply(currentMask, operations,
                    definition.Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal));
                if (restoredProjection != originalMask)
                    throw new InvalidOperationException("A bit outside the tested option set changed; restoration would overwrite an unrelated value.");
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValue), JsonNullValue(), operations));
            }
            else if (IsBitDropdown(id))
            {
                var originalBoolean = (bool)originalValue;
                var currentBoolean = (bool)currentValue;
                restoreChanges.Add(new SettingChange(id, JsonValue(currentBoolean), JsonValue(originalBoolean ? "1" : "0"), null));
            }
            else if (IsIntegerCheckbox(id))
            {
                var originalBoolean = string.Equals(Convert.ToString(originalValue, CultureInfo.InvariantCulture), "1", StringComparison.Ordinal);
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValue), JsonValue(originalBoolean), null));
            }
            else
            {
                restoreChanges.Add(new SettingChange(id, JsonValue(currentValue), JsonValue(originalValue), null));
            }
        }

        if (restoreChanges.Count > 0)
        {
            var restoreResult = await service.SaveAsync(new PersistentSettingsSaveRequest(null, null, Changes: restoreChanges), true, CancellationToken.None);
            if (!restoreResult.Committed)
                throw new InvalidOperationException("The isolated settings restoration was not confirmed.");
        }

        var restored = await store.ReadAsync(CancellationToken.None);
        foreach (var id in parameterIds)
            Assert.True(SettingUpdateValidator.ValuesEqual(restored.Items.Single(item => item.ParameterId == id).Value, originalValues[id]),
                "A tested setting was not restored to its original state.");
    }

    private static (IReadOnlyList<SettingChange> Changes, IReadOnlyDictionary<string, object?> ExpectedValues) BuildChanges(
        IReadOnlyList<string> parameterIds,
        IReadOnlyDictionary<string, object?> originalValues,
        IReadOnlyDictionary<string, ParameterDefinition> definitions)
    {
        var changes = new List<SettingChange>();
        var expectedValues = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var id in parameterIds)
        {
            var definition = definitions[id];
            var current = originalValues[id];
            if (current is null) throw new InvalidOperationException("An isolated candidate value is NULL; it cannot be round-trip tested without changing NULL semantics.");

            if (definition.ControlType == "bitmask")
            {
                var mask = ParseMask(current);
                var option = definition.Options.FirstOrDefault()
                    ?? throw new InvalidOperationException("A candidate bitmask has no source options.");
                var enabled = !IsSet(mask, option.Value);
                var operation = new BitOperation(option.Value, enabled);
                expectedValues[id] = BitmaskEditor.Apply(mask, [operation],
                    definition.Options.Select(item => item.Value).ToHashSet(StringComparer.Ordinal)).ToString(CultureInfo.InvariantCulture);
                changes.Add(new SettingChange(id, JsonValue(current), JsonNullValue(), [operation]));
            }
            else if (IsBitDropdown(id))
            {
                if (current is not bool currentBoolean) throw new InvalidOperationException("A SQL bit dropdown did not load as a Boolean value.");
                var nextText = currentBoolean ? "0" : "1";
                if (!definition.Options.Any(option => option.Value == nextText)) throw new InvalidOperationException("The Boolean dropdown options are not 0 and 1.");
                expectedValues[id] = !currentBoolean;
                changes.Add(new SettingChange(id, JsonValue(currentBoolean), JsonValue(nextText), null));
            }
            else if (IsIntegerCheckbox(id))
            {
                var currentText = Convert.ToString(current, CultureInfo.InvariantCulture);
                if (currentText is not ("0" or "1")) throw new InvalidOperationException("The integer checkbox value is not 0 or 1.");
                var nextBoolean = currentText == "0";
                expectedValues[id] = nextBoolean ? "1" : "0";
                changes.Add(new SettingChange(id, JsonValue(currentText), JsonValue(nextBoolean), null));
            }
            else if (IsTextScalar(id))
            {
                var currentText = Convert.ToString(current, CultureInfo.InvariantCulture)!;
                var nextText = string.Equals(currentText, "qa-test", StringComparison.Ordinal) ? "qa-test-value" : "qa-test";
                expectedValues[id] = nextText;
                changes.Add(new SettingChange(id, JsonValue(currentText), JsonValue(nextText), null));
            }
            else if (id is "ctl00_PageContent_txtMinCartCount" or "ctl00_PageContent_txtMaximumOrderQuantity" or "ctl00_PageContent_txtMinCartTotal")
            {
                var currentText = Convert.ToString(current, CultureInfo.InvariantCulture)!;
                var nextText = id == "ctl00_PageContent_txtMinCartTotal"
                    ? currentText == "0.01" ? "0.02" : "0.01"
                    : currentText == "1" ? "2" : "1";
                expectedValues[id] = nextText;
                changes.Add(new SettingChange(id, JsonValue(currentText), JsonValue(nextText), null));
            }
            else if (definition.ControlType == "checkbox")
            {
                if (current is not bool currentBoolean)
                    throw new InvalidOperationException("A checkbox did not load as a Boolean value.");
                var nextValue = !currentBoolean;
                expectedValues[id] = nextValue;
                changes.Add(new SettingChange(id, JsonValue(current), JsonValue(nextValue), null));
            }
            else
            {
                var currentText = Convert.ToString(current, CultureInfo.InvariantCulture)!;
                if (!definition.Options.Any(option => string.Equals(option.Value, currentText, StringComparison.Ordinal)))
                    throw new InvalidOperationException("A scalar test value is not represented in the original control options.");
                var next = definition.Options.FirstOrDefault(option => !string.Equals(option.Value, currentText, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("A scalar control has no alternate source option for a safe test.");
                expectedValues[id] = next.Value;
                changes.Add(new SettingChange(id, JsonValue(current), JsonValue(next.Value), null));
            }
        }
        return (changes, expectedValues);
    }

    private static bool IsSet(long mask, string optionValue) => (mask & long.Parse(optionValue, NumberStyles.None, CultureInfo.InvariantCulture)) != 0;

    private static bool IsBitDropdown(string id) => id is "ctl00_PageContent_ddlCurrencyType" or "ctl00_PageContent_ddlExpiryDate"
        or "ctl00_PageContent_ddlintegrated" or "ctl00_PageContent_ddlBonusShow";

    private static bool IsIntegerCheckbox(string id) => id == "ctl00_PageContent_chkDoNotSendMailToCustomer";

    private static string CatalogWriteColumn(string id) => id switch
    {
        "ctl00_PageContent_ddlCurrencyType" => "UseCurrencyType",
        "ctl00_PageContent_ddlExpiryDate" => "ExpiryDate",
        "ctl00_PageContent_ddlintegrated" => "Integrated",
        "ctl00_PageContent_ddlBonusShow" => "BonusShow",
        _ => throw new InvalidOperationException("The SQL bit dropdown is not allowlisted."),
    };

    private static async Task AssertSqlBaselineMatchesAsync(string connectionString, string column, object? baseline)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand($"SELECT COUNT_BIG(*) FROM dbo.DealerSettings WHERE MemberCode=@memberCode AND (([{column}]=@expected) OR ([{column}] IS NULL AND @expected IS NULL));", connection);
        command.Parameters.AddWithValue("@memberCode", "store5");
        command.Parameters.Add(SqlValueConverter.CreateParameter("@expected", "bit", baseline));
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
    }

    private static bool IsTextScalar(string id) => id is "ctl00_PageContent_txtProductImageAspectRatio" or "ctl00_PageContent_txtLayoutColor"
        or "ctl00_PageContent_txtLayoutColor2" or "ctl00_PageContent_txtGoogleAnalysticScript" or "ctl00_PageContent_txtGoogleAnalysticProfileID"
        or "ctl00_PageContent_txtGoogleAdWordsReMarketingCode" or "ctl00_PageContent_txtMinStockFilter" or "ctl00_PageContent_txtRegisterFormUrl"
        or "ctl00_PageContent_txtBayiTelefon" or "ctl00_PageContent_txtBayiAdres" or "ctl00_PageContent_txtPaymentLink"
        or "ctl00_PageContent_TxtDealerName" or "ctl00_PageContent_txtBayiYetkiliKisi" or "ctl00_PageContent_txtBayiWebAdres"
        or "ctl00_PageContent_txtEInvoiceNo";

    private static long ParseMask(object value) => long.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static JsonElement JsonValue(object? value) => JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement.Clone();
    private static JsonElement JsonNullValue() => JsonDocument.Parse("null").RootElement.Clone();

    private static object? NormalizeJsonValue(object? value) => value is JsonElement element ? element.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        _ => value,
    } : value;
}
