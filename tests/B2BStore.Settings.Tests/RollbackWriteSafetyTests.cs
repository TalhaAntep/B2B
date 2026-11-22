using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public class RollbackWriteSafetyTests
{
    private const string SiteStatusId = "ctl00_PageContent_ddlSiteStatus";
    private const string ParameterOrderId = "ctl00_PageContent_ddlOrderParameter";
    private const string TestFlagName = "B2BSTORE_SQL_WRITE_TEST_MODE";

    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));

    [Fact]
    public async Task RollbackPathRequiresServerAuthorizationAndExplicitTestMode()
    {
        var connection = RequiredConnection();
        var request = new RollbackOnlySettingsTestRequest(new SiteStatusRollbackChange(null, 1), null);

        var unauthenticated = new SqlRollbackOnlySettingsTestService(Catalog, connection, "store5", true);
        var authError = await Assert.ThrowsAsync<SettingsException>(() => unauthenticated.RunAsync(request, false, CancellationToken.None));
        Assert.Equal("TestWriteUnauthorized", authError.Code);

        var disabled = new SqlRollbackOnlySettingsTestService(Catalog, connection, "store5", false);
        var modeError = await Assert.ThrowsAsync<SettingsException>(() => disabled.RunAsync(request, true, CancellationToken.None));
        Assert.Equal("TestWriteModeRequired", modeError.Code);
    }

    [Fact]
    public async Task Store5AnalysisAndOtherMembersAreRejectedBeforeConnection()
    {
        var builder = new SqlConnectionStringBuilder(RequiredConnection()) { InitialCatalog = "Store5_Analysis" };
        var analysis = new SqlRollbackOnlySettingsTestService(Catalog, builder.ConnectionString, "store5", true);
        var isolationError = await Assert.ThrowsAsync<SettingsException>(() => analysis.RunAsync(
            new RollbackOnlySettingsTestRequest(new SiteStatusRollbackChange(null, 1), null), true, CancellationToken.None));
        Assert.Equal("DatabaseIsolationViolation", isolationError.Code);

        var disabledAnalysis = new SqlRollbackOnlySettingsTestService(Catalog, builder.ConnectionString, "store5", false);
        var disabledError = await Assert.ThrowsAsync<SettingsException>(() => disabledAnalysis.RunAsync(
            new RollbackOnlySettingsTestRequest(new SiteStatusRollbackChange(null, 1), null), true, CancellationToken.None));
        Assert.Equal("TestWriteModeRequired", disabledError.Code);

        var otherMember = new SqlRollbackOnlySettingsTestService(Catalog, RequiredConnection(), "not-store5", true);
        var memberError = await Assert.ThrowsAsync<SettingsException>(() => otherMember.RunAsync(
            new RollbackOnlySettingsTestRequest(new SiteStatusRollbackChange(null, 1), null), true, CancellationToken.None));
        Assert.Equal("DatabaseIsolationViolation", memberError.Code);
    }

    [Fact]
    public async Task InvalidStatusAndBitOptionsAreRejected()
    {
        var service = CreateEnabledService();
        var badStatus = await Assert.ThrowsAsync<SettingsException>(() => service.RunAsync(
            new RollbackOnlySettingsTestRequest(new SiteStatusRollbackChange(null, 2), null), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", badStatus.Code);

        var badMask = await Assert.ThrowsAsync<SettingsException>(() => service.RunAsync(
            new RollbackOnlySettingsTestRequest(null, new ParameterOrderRollbackChange(null, [new BitOperation("3", true)])), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", badMask.Code);

        var badExpected = await Assert.ThrowsAsync<SettingsException>(() => service.RunAsync(
            new RollbackOnlySettingsTestRequest(null, new ParameterOrderRollbackChange("9223372036854775808", [new BitOperation("1", true)])), true, CancellationToken.None));
        Assert.Equal("ValidationFailed", badExpected.Code);
    }

    [Fact]
    public async Task SiteStatusAndParameterOrderReadBackInsideTransactionThenRollbackWithoutLosingBits()
    {
        Assert.True(string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase),
            "SQL write tests require the explicit local rollback-test flag.");
        var catalog = Catalog;
        var before = await ReadStore5Async(catalog);
        var originalSiteStatus = ReadNullableInt(before, SiteStatusId);
        Assert.True(originalSiteStatus is null or 0 or 1, "The current SiteStatus is outside the supported Active/Inactive values.");
        var newSiteStatus = originalSiteStatus == 1 ? 0 : 1;

        var originalOrder = ReadNullableString(before, ParameterOrderId);
        var originalMask = originalOrder is null ? 0L : long.Parse(originalOrder, CultureInfo.InvariantCulture);
        var inventory = catalog.AllSettings.Single(item => item.ParameterId == ParameterOrderId);
        var selectedFlag = inventory.Options.Single(option => option.Value == "1");
        Assert.Equal("Request Contract Approval", selectedFlag.Label);
        var flagCurrentlyEnabled = (originalMask & 1L) != 0;

        var request = new RollbackOnlySettingsTestRequest(
            new SiteStatusRollbackChange(originalSiteStatus, newSiteStatus),
            new ParameterOrderRollbackChange(originalOrder, [new BitOperation(selectedFlag.Value, !flagCurrentlyEnabled)]));
        var response = await CreateEnabledService(catalog).RunAsync(request, true, CancellationToken.None);

        Assert.True(response.RolledBack);
        Assert.True(response.SiteStatusBefore == originalSiteStatus, "The rollback response did not report the original SiteStatus.");
        Assert.True(response.SiteStatusInsideTransaction == newSiteStatus, "The transaction did not expose the temporary SiteStatus value.");
        Assert.True(response.SiteStatusAfterRollback == originalSiteStatus, "The rollback did not restore the original SiteStatus.");
        Assert.True(string.Equals(originalOrder, response.ParameterOrderBefore, StringComparison.Ordinal), "The rollback response did not preserve the original ParameterOrder value.");
        Assert.True(string.Equals(originalOrder, response.ParameterOrderAfterRollback, StringComparison.Ordinal), "The rollback did not restore the original ParameterOrder value.");
        Assert.NotNull(response.ParameterOrderInsideTransaction);

        var changedMask = long.Parse(response.ParameterOrderInsideTransaction!, CultureInfo.InvariantCulture);
        Assert.True((originalMask ^ 1L) == changedMask, "The temporary ParameterOrder update did not toggle exactly the selected test bit.");
        Assert.True((originalMask & ~1L) == (changedMask & ~1L), "The temporary ParameterOrder update changed an unrelated known bit.");
        var knownMask = inventory.Options.Aggregate(0L, (mask, option) => mask | long.Parse(option.Value, CultureInfo.InvariantCulture));
        Assert.True((originalMask & ~knownMask) == (changedMask & ~knownMask), "The temporary ParameterOrder update changed an unknown bit.");

        var afterRollbackRead = await ReadStore5Async(catalog);
        Assert.True(originalSiteStatus == ReadNullableInt(afterRollbackRead, SiteStatusId), "The database read after rollback did not match the original SiteStatus.");
        Assert.True(string.Equals(originalOrder, ReadNullableString(afterRollbackRead, ParameterOrderId), StringComparison.Ordinal), "The post-rollback database read did not match the original ParameterOrder.");
    }

    [Fact]
    public async Task StaleSecondFieldCausesConcurrencyConflictAndRollsBackEarlierUpdate()
    {
        var catalog = Catalog;
        var before = await ReadStore5Async(catalog);
        var originalSiteStatus = ReadNullableInt(before, SiteStatusId);
        Assert.True(originalSiteStatus is null or 0 or 1);
        var originalOrder = ReadNullableString(before, ParameterOrderId);
        var mask = originalOrder is null ? 0L : long.Parse(originalOrder, CultureInfo.InvariantCulture);
        var staleOrder = originalOrder == "0" ? "1" : "0";
        var request = new RollbackOnlySettingsTestRequest(
            new SiteStatusRollbackChange(originalSiteStatus, originalSiteStatus == 1 ? 0 : 1),
            new ParameterOrderRollbackChange(staleOrder, [new BitOperation("1", (mask & 1L) == 0)]));

        var error = await Assert.ThrowsAsync<SettingsException>(() => CreateEnabledService(catalog).RunAsync(request, true, CancellationToken.None));
        Assert.Equal("ConcurrencyConflict", error.Code);

        var after = await ReadStore5Async(catalog);
        Assert.True(ReadNullableInt(before, SiteStatusId) == ReadNullableInt(after, SiteStatusId), "The concurrency rollback changed the original SiteStatus.");
        Assert.True(string.Equals(ReadNullableString(before, ParameterOrderId), ReadNullableString(after, ParameterOrderId), StringComparison.Ordinal),
            "A concurrency rollback changed the original ParameterOrder value.");
    }

    private static SqlRollbackOnlySettingsTestService CreateEnabledService(ParameterCatalog? catalog = null) => new(
        catalog ?? Catalog,
        RequiredConnection(),
        "store5",
        string.Equals(Environment.GetEnvironmentVariable(TestFlagName), "true", StringComparison.OrdinalIgnoreCase));

    private static string RequiredConnection()
    {
        var connection = Environment.GetEnvironmentVariable("B2BSTORE_SQL_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection), "Read-only SQL integration connection is required for this test run.");
        return connection!;
    }

    private static async Task<SettingsResponse> ReadStore5Async(ParameterCatalog catalog)
    {
        var store = new SqlServerSettingsStore(catalog, RequiredConnection(), "store5");
        return await store.ReadAsync(CancellationToken.None);
    }

    private static int? ReadNullableInt(SettingsResponse response, string parameterId)
    {
        var value = response.Items.Single(item => item.ParameterId == parameterId).Value;
        return value is null ? null : int.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
    }

    private static string? ReadNullableString(SettingsResponse response, string parameterId) =>
        response.Items.Single(item => item.ParameterId == parameterId).Value is { } value
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;
}
