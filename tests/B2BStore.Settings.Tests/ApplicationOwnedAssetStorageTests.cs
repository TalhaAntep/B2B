using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class ApplicationOwnedAssetStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "b2b-assets-tests", Guid.NewGuid().ToString("N"));
    private const string LogoId = "ctl00_PageContent_imgLogo_imageUpload";
    private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0];

    [Fact]
    public async Task SavesReplacesReadsAndDeletesApplicationOnlyFilesWithGeneratedNames()
    {
        var storage = new ApplicationOwnedAssetStorage(root);
        var saved = await SaveAsync(storage, LogoId, "../../private-name.png", PngHeader);
        Assert.NotEqual("private-name.png", saved.StoredFileName);
        Assert.Matches("^[0-9a-f]{32}\\.png$", saved.StoredFileName);
        Assert.Equal(PngHeader.Length, saved.Length);

        var afterRestart = new ApplicationOwnedAssetStorage(root);
        var read = await afterRestart.ReadAsync(LogoId, CancellationToken.None);
        Assert.NotNull(read);
        Assert.Equal(PngHeader, read.Content);
        Assert.Equal("image/png", read.Metadata.ContentType);

        var replacement = PngHeader.Concat(new byte[] { 4 }).ToArray();
        var replaced = await SaveAsync(afterRestart, LogoId, "replacement.png", replacement);
        Assert.NotEqual(saved.StoredFileName, replaced.StoredFileName);
        Assert.Equal(replacement, (await storage.ReadAsync(LogoId, CancellationToken.None))!.Content);
        Assert.True(await storage.DeleteAsync(LogoId, CancellationToken.None));
        Assert.Null(await storage.ReadAsync(LogoId, CancellationToken.None));
        Assert.False(await storage.DeleteAsync(LogoId, CancellationToken.None));
    }

    [Fact]
    public async Task RejectsUnknownSlotsUnsupportedMimeAndOversizedUploads()
    {
        var storage = new ApplicationOwnedAssetStorage(root);
        await Assert.ThrowsAsync<SettingsException>(() => SaveAsync(storage, "../../other-member", "a.png", PngHeader));
        await Assert.ThrowsAsync<SettingsException>(() => storage.SaveAsync(LogoId, "a.png", "image/jpeg",
            new MemoryStream(PngHeader), PngHeader.Length, CancellationToken.None));
        await Assert.ThrowsAsync<SettingsException>(() => storage.SaveAsync(LogoId, "a.svg", "image/svg+xml",
            new MemoryStream(PngHeader), PngHeader.Length, CancellationToken.None));
        await Assert.ThrowsAsync<SettingsException>(() => storage.SaveAsync(LogoId, "a.png", "image/png",
            new MemoryStream([1]), ApplicationOwnedAssetStorage.MaximumBytes + 1, CancellationToken.None));
    }

    [Fact]
    public async Task RejectsMismatchedFileSignatures()
    {
        var storage = new ApplicationOwnedAssetStorage(root);
        await Assert.ThrowsAsync<SettingsException>(() => SaveAsync(storage, LogoId, "a.png", [1, 2, 3, 4]));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
        catch (IOException) { }
    }

    private static Task<ApplicationAssetMetadata> SaveAsync(ApplicationOwnedAssetStorage storage,
        string parameterId, string fileName, byte[] content) => storage.SaveAsync(parameterId, fileName, "image/png",
            new MemoryStream(content), content.Length, CancellationToken.None);
}
