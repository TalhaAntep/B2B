using System.Text.Json;

namespace B2BStore.Settings.Api;

public sealed record ApplicationAssetMetadata(string ParameterId, string StoredFileName, string ContentType, long Length);
public sealed record StoredApplicationAsset(ApplicationAssetMetadata Metadata, byte[] Content);

/// <summary>Local application-owned uploads. These files are intentionally not written to DealerSettings.</summary>
public sealed class ApplicationOwnedAssetStorage(string storageRoot)
{
    public const long MaximumBytes = 5 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, (string Extension, string ContentType)> SupportedTypes =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = (".png", "image/png"),
            [".jpg"] = (".jpg", "image/jpeg"),
            [".jpeg"] = (".jpg", "image/jpeg"),
            [".webp"] = (".webp", "image/webp"),
            [".ico"] = (".ico", "image/x-icon"),
        };
    private static readonly HashSet<string> SupportedIds = new(StringComparer.Ordinal)
    {
        "ctl00_PageContent_imgLogo_imageUpload", "ctl00_PageContent_imgLogo2_imageUpload",
        "ctl00_PageContent_imgLogoLogin_imageUpload", "ctl00_PageContent_imgFavicon_imageUpload",
        "ctl00_PageContent_imgNoImage_imageUpload",
    };
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly string root = Path.GetFullPath(storageRoot);
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);

    public async Task<ApplicationAssetMetadata> SaveAsync(string parameterId, string fileName, string contentType,
        Stream content, long contentLength, CancellationToken cancellationToken)
    {
        EnsureSupportedId(parameterId);
        if (contentLength <= 0 || contentLength > MaximumBytes)
            throw new SettingsException("InvalidAsset", "Image uploads must be between 1 byte and 5 MiB.");
        var extension = Path.GetExtension(fileName);
        if (!SupportedTypes.TryGetValue(extension, out var format))
            throw new SettingsException("InvalidAsset", "Only PNG, JPEG, WebP, and ICO images are supported.");
        if (!string.Equals(contentType, format.ContentType, StringComparison.OrdinalIgnoreCase)
            && !(extension.Equals(".ico", StringComparison.OrdinalIgnoreCase) && contentType == "image/vnd.microsoft.icon"))
            throw new SettingsException("InvalidAsset", "The file type and MIME type do not match.");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length != contentLength || !HasSignature(buffer.GetBuffer().AsSpan(0, checked((int)buffer.Length)), format.ContentType))
            throw new SettingsException("InvalidAsset", "The uploaded content is not a valid supported image type.");

        await gate.WaitAsync(cancellationToken);
        try
        {
            var folder = GetDealerFolder();
            Directory.CreateDirectory(folder);
            var metadataPath = Path.Combine(folder, "assets.json");
            var entries = await ReadMetadataAsync(metadataPath, cancellationToken);
            entries.TryGetValue(parameterId, out var previous);
            var storedName = $"{Guid.NewGuid():N}{format.Extension}";
            var finalPath = SafeFilePath(folder, storedName);
            var tempPath = SafeFilePath(folder, $".{Guid.NewGuid():N}.upload");
            var tempMetadataPath = SafeFilePath(folder, $".{Guid.NewGuid():N}.json");
            try
            {
                await File.WriteAllBytesAsync(tempPath, buffer.ToArray(), cancellationToken);
                entries[parameterId] = new ApplicationAssetMetadata(parameterId, storedName, format.ContentType, buffer.Length);
                await File.WriteAllTextAsync(tempMetadataPath, JsonSerializer.Serialize(entries, json), cancellationToken);
                File.Move(tempPath, finalPath);
                File.Move(tempMetadataPath, metadataPath, true);
            }
            catch
            {
                TryDelete(tempPath);
                TryDelete(tempMetadataPath);
                TryDelete(finalPath);
                throw;
            }
            if (previous is not null) TryDelete(SafeFilePath(folder, previous.StoredFileName));
            return entries[parameterId];
        }
        finally { gate.Release(); }
    }

    public async Task<StoredApplicationAsset?> ReadAsync(string parameterId, CancellationToken cancellationToken)
    {
        EnsureSupportedId(parameterId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var folder = GetDealerFolder();
            var entries = await ReadMetadataAsync(Path.Combine(folder, "assets.json"), cancellationToken);
            if (!entries.TryGetValue(parameterId, out var metadata)) return null;
            var bytes = await File.ReadAllBytesAsync(SafeFilePath(folder, metadata.StoredFileName), cancellationToken);
            return new StoredApplicationAsset(metadata, bytes);
        }
        catch (DirectoryNotFoundException) { return null; }
        finally { gate.Release(); }
    }

    public async Task<IReadOnlyDictionary<string, ApplicationAssetMetadata>> ListAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try { return await ReadMetadataAsync(Path.Combine(GetDealerFolder(), "assets.json"), cancellationToken); }
        catch (DirectoryNotFoundException) { return new Dictionary<string, ApplicationAssetMetadata>(StringComparer.Ordinal); }
        finally { gate.Release(); }
    }

    public async Task<bool> DeleteAsync(string parameterId, CancellationToken cancellationToken)
    {
        EnsureSupportedId(parameterId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var folder = GetDealerFolder();
            var metadataPath = Path.Combine(folder, "assets.json");
            var entries = await ReadMetadataAsync(metadataPath, cancellationToken);
            if (!entries.Remove(parameterId, out var previous)) return false;
            var tempMetadataPath = SafeFilePath(folder, $".{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(tempMetadataPath, JsonSerializer.Serialize(entries, json), cancellationToken);
            File.Move(tempMetadataPath, metadataPath, true);
            TryDelete(SafeFilePath(folder, previous.StoredFileName));
            return true;
        }
        catch (DirectoryNotFoundException) { return false; }
        finally { gate.Release(); }
    }

    private string GetDealerFolder() => Path.Combine(root, "store5");

    private static void EnsureSupportedId(string parameterId)
    {
        if (!SupportedIds.Contains(parameterId)) throw new SettingsException("InvalidAsset", "This control is not an application-owned image slot.");
    }

    private static string SafeFilePath(string folder, string fileName)
    {
        if (Path.GetFileName(fileName) != fileName || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new SettingsException("InvalidAsset", "An asset path is invalid.");
        var candidate = Path.GetFullPath(Path.Combine(folder, fileName));
        if (!candidate.StartsWith(Path.GetFullPath(folder) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new SettingsException("InvalidAsset", "An asset path is outside the application storage directory.");
        return candidate;
    }

    private async Task<Dictionary<string, ApplicationAssetMetadata>> ReadMetadataAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path)) return new Dictionary<string, ApplicationAssetMetadata>(StringComparer.Ordinal);
        var content = await File.ReadAllTextAsync(path, token);
        var entries = JsonSerializer.Deserialize<Dictionary<string, ApplicationAssetMetadata>>(content, json)
            ?? throw new SettingsException("AssetStorageUnavailable", "Application asset metadata is invalid.");
        foreach (var (id, metadata) in entries)
        {
            EnsureSupportedId(id);
            if (metadata.ParameterId != id || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(metadata.StoredFileName), "N", out _)
                || !SupportedTypes.Values.Any(type => type.Extension == Path.GetExtension(metadata.StoredFileName)
                    && type.ContentType == metadata.ContentType)
                || metadata.Length <= 0 || metadata.Length > MaximumBytes)
                throw new SettingsException("AssetStorageUnavailable", "Application asset metadata failed validation.");
        }
        return entries;
    }

    private static bool HasSignature(ReadOnlySpan<byte> bytes, string contentType) => contentType switch
    {
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        "image/x-icon" => bytes.Length >= 4 && bytes[0] == 0 && bytes[1] == 0 && bytes[2] == 1 && bytes[3] == 0,
        _ => false,
    };

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
