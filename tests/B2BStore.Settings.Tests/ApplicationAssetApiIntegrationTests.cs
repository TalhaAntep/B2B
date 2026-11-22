using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class ApplicationAssetApiIntegrationTests
{
    private const string Slot = "ctl00_PageContent_imgLogo_imageUpload";
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0];

    [Fact]
    public async Task AuthenticatedTestApiStoresAndRemovesApplicationOnlyAsset()
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("B2BSTORE_SQL_WRITE_TEST_MODE")?.ToLowerInvariant());
        var url = Environment.GetEnvironmentVariable("B2BSTORE_TEST_API_URL");
        var token = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN");
        var connection = Environment.GetEnvironmentVariable("B2BSTORE_SQL_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(url));
        Assert.False(string.IsNullOrWhiteSpace(token));
        RollbackWriteTestGuard.EnsureAllowed(true, true, "store5", connection);

        using var client = new HttpClient { BaseAddress = new Uri(url!) };
        using (var unauthenticated = await client.GetAsync("/api/application-assets"))
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var before = await client.GetFromJsonAsync<AssetList>("/api/application-assets");
        Assert.NotNull(before);
        Assert.False(before.Assets.ContainsKey(Slot), "The test slot already contains an app-owned asset; refusing to replace it.");

        var saved = false;
        try
        {
            using var form = new MultipartFormDataContent();
            var image = new ByteArrayContent(Png);
            image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(image, "file", "synthetic.png");
            using var response = await client.PostAsync($"/api/application-assets/{Uri.EscapeDataString(Slot)}", form);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            saved = true;
            var savedBody = await response.Content.ReadFromJsonAsync<AssetSaveResult>();
            Assert.True(savedBody!.ApplicationOnly);
            Assert.False(savedBody.LegacyPortalCompatible);
            Assert.Equal("image/png", savedBody.Metadata.ContentType);
            Assert.Matches("^[0-9a-f]{32}\\.png$", savedBody.Metadata.StoredFileName);

            var listed = await client.GetFromJsonAsync<AssetList>("/api/application-assets");
            Assert.True(listed!.Assets.ContainsKey(Slot));
            var content = await client.GetByteArrayAsync($"/api/application-assets/{Uri.EscapeDataString(Slot)}/content");
            Assert.Equal(Png, content);

            using var delete = await client.DeleteAsync($"/api/application-assets/{Uri.EscapeDataString(Slot)}");
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
            saved = false;
            Assert.False((await client.GetFromJsonAsync<AssetList>("/api/application-assets"))!.Assets.ContainsKey(Slot));
        }
        finally
        {
            if (saved)
            {
                using var cleanup = await client.DeleteAsync($"/api/application-assets/{Uri.EscapeDataString(Slot)}");
                if (!cleanup.IsSuccessStatusCode) throw new InvalidOperationException("Application-owned asset cleanup failed.");
            }
        }
    }

    private sealed record AssetList(Dictionary<string, ApplicationAssetMetadata> Assets, bool ApplicationOnly, bool LegacyPortalCompatible);
    private sealed record AssetSaveResult(ApplicationAssetMetadata Metadata, bool ApplicationOnly, bool LegacyPortalCompatible);
}
