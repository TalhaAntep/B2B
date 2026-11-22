using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public class SqlReadOnlyIntegrationTests
{
    [Fact]
    public async Task ReadsOnlyConfiguredStoreRowAndLoadsValuesWithoutEnablingEdits()
    {
        var connection = Environment.GetEnvironmentVariable("B2BSTORE_SQL_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connection), "Read-only SQL integration connection is required for this test run.");
        var catalog = ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));
        var store = new SqlServerSettingsStore(catalog, connection, "store5");
        var response = await store.ReadAsync(CancellationToken.None);
        Assert.Equal("SqlServer", response.Mode);
        Assert.Equal(98, response.Items.Count);
        Assert.All(response.Items, item => Assert.False(item.Editable));
        Assert.Equal(92, response.Items.Count(item => item.MappingStatus == "candidate"));
        Assert.Equal(6, response.Items.Count(item => item.MappingStatus == "unresolved"));
        Assert.Contains(response.Items, item => item.MappingStatus == "candidate" && item.Value is not null);
        var orderMask = response.Items.Single(item => item.ParameterId == "ctl00_PageContent_ddlOrderParameter");
        if (orderMask.Value is not null) Assert.IsType<string>(orderMask.Value);
    }

    [Fact]
    public async Task SqlStoreRejectsWritesBeforeOpeningDatabaseConnection()
    {
        var catalog = ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));
        var store = new SqlServerSettingsStore(catalog, null, null);
        var error = await Assert.ThrowsAsync<SettingsException>(() => store.UpdateAsync(new UpdateRequest([]), CancellationToken.None));
        Assert.Equal("ReadOnlyMode", error.Code);
    }
}
