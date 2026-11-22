using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using B2BStore.Settings.Api;

var builder = WebApplication.CreateBuilder(args);
var localSettingsPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "B2BStore",
    "settings.local.json");
if (File.Exists(localSettingsPath))
    builder.Configuration.AddJsonFile(localSettingsPath, optional: true, reloadOnChange: false);
var catalogPath = Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json");
var catalog = ParameterCatalog.Load(catalogPath);
var mode = Environment.GetEnvironmentVariable("B2BSTORE_MODE")
    ?? builder.Configuration["B2BStore:Mode"]
    ?? (builder.Environment.IsDevelopment() ? "Mock" : "SqlServer");
var adminToken = Environment.GetEnvironmentVariable("B2BSTORE_ADMIN_TOKEN") ?? builder.Configuration["B2BStore:AdminToken"];
var connectionString = Environment.GetEnvironmentVariable("B2BSTORE_SQL_CONNECTION") ?? builder.Configuration["B2BStore:SqlConnection"];
var memberCode = Environment.GetEnvironmentVariable("B2BSTORE_MEMBER_CODE") ?? builder.Configuration["B2BStore:MemberCode"];
var writeTestFlag = string.Equals(Environment.GetEnvironmentVariable("B2BSTORE_SQL_WRITE_TEST_MODE"), "true", StringComparison.OrdinalIgnoreCase);
var writeTestModeEnabled = writeTestFlag && builder.Environment.IsDevelopment()
    && string.Equals(mode, "SqlServer", StringComparison.OrdinalIgnoreCase);
var settingsWriteMode = Environment.GetEnvironmentVariable("B2BSTORE_SETTINGS_WRITE_MODE");
var isolatedLocalWriteModeEnabled = string.Equals(settingsWriteMode, "IsolatedLocalDemo", StringComparison.Ordinal)
    && builder.Environment.IsDevelopment()
    && string.Equals(mode, "SqlServer", StringComparison.OrdinalIgnoreCase);
var applicationWriteModeEnabled = writeTestModeEnabled || isolatedLocalWriteModeEnabled;

if (writeTestFlag && !writeTestModeEnabled)
    throw new InvalidOperationException("B2BSTORE_SQL_WRITE_TEST_MODE is available only in Development with SqlServer mode.");
if (settingsWriteMode is not null && !isolatedLocalWriteModeEnabled)
    throw new InvalidOperationException("B2BSTORE_SETTINGS_WRITE_MODE=IsolatedLocalDemo is available only in Development with SqlServer mode.");
if (isolatedLocalWriteModeEnabled)
    RollbackWriteTestGuard.EnsureAllowed(true, true, memberCode, connectionString);

if (!string.Equals(mode, "Mock", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(mode, "SqlServer", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("B2BSTORE_MODE must be Mock or SqlServer.");
}

if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(adminToken)) throw new InvalidOperationException("B2BSTORE_ADMIN_TOKEN is required in Production.");
    if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("B2BSTORE_SQL_CONNECTION is required in Production.");
    if (string.IsNullOrWhiteSpace(memberCode)) throw new InvalidOperationException("B2BSTORE_MEMBER_CODE is required in Production.");
    if (string.Equals(mode, "Mock", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Mock mode is not allowed in Production.");
}

builder.Services.AddSingleton(catalog);
builder.Services.AddSingleton<ISettingsStore>(services =>
    string.Equals(mode, "SqlServer", StringComparison.OrdinalIgnoreCase)
        ? new SqlServerSettingsStore(catalog, connectionString, memberCode)
        : new MockSettingsStore(catalog));
builder.Services.AddCors(options => options.AddPolicy("local-ui", policy => policy
    .WithOrigins("http://127.0.0.1:5173", "http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

var app = builder.Build();
app.UseCors("local-ui");
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/health")
    {
        await next();
        return;
    }

    if (string.IsNullOrWhiteSpace(adminToken))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new ApiError("AdminTokenMissing", "Set B2BSTORE_ADMIN_TOKEN before using the settings API."));
        return;
    }

    var authorization = context.Request.Headers.Authorization.ToString();
    var supplied = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authorization[7..].Trim()
        : string.Empty;
    if (!FixedTimeEquals(adminToken, supplied))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ApiError("Unauthorized", "A valid local admin token is required."));
        return;
    }
    context.Items["B2BStoreAdminAuthorized"] = true;
    await next();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/settings/catalog", (ParameterCatalog loaded) => Results.Ok(loaded));
app.MapGet("/api/settings/capabilities", () => Results.Ok(new
{
    canSaveSettings = isolatedLocalWriteModeEnabled,
    canManageApplicationAssets = isolatedLocalWriteModeEnabled,
    environment = isolatedLocalWriteModeEnabled ? "IsolatedLocalDemo" : "ReadOnly",
    target = isolatedLocalWriteModeEnabled ? "Store5_Settings_Test / store5" : null,
}));
app.MapGet("/api/settings", async (ISettingsStore store, CancellationToken cancellationToken) =>
{
    try
    {
        var result = await store.ReadAsync(cancellationToken);
        return Results.Ok(result with { TestWritesEnabled = isolatedLocalWriteModeEnabled });
    }
    catch (SettingsException exception) { return SettingsError(exception); }
    catch (Exception) { return Results.Problem("Settings could not be read.", statusCode: 503); }
});
if (applicationWriteModeEnabled)
{
    var assetRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "B2BStore", "application-assets");
    var assetStorage = new ApplicationOwnedAssetStorage(assetRoot);
    app.MapPost("/api/application-assets/{parameterId}", async (
        string parameterId, HttpRequest request, HttpContext context, CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try
        {
            RollbackWriteTestGuard.EnsureAllowed(applicationWriteModeEnabled, authenticated, memberCode, connectionString);
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null) return Results.BadRequest(new ApiError("InvalidAsset", "Choose an image file to upload."));
            await using var stream = file.OpenReadStream();
            var metadata = await assetStorage.SaveAsync(parameterId, file.FileName, file.ContentType, stream, file.Length, cancellationToken);
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(new { metadata, applicationOnly = true, legacyPortalCompatible = false });
        }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("The application-owned image could not be stored.", statusCode: 503); }
    });
    app.MapGet("/api/application-assets/{parameterId}", async (
        string parameterId, HttpContext context, CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try
        {
            RollbackWriteTestGuard.EnsureAllowed(applicationWriteModeEnabled, authenticated, memberCode, connectionString);
            var asset = await assetStorage.ReadAsync(parameterId, cancellationToken);
            if (asset is null) return Results.NotFound();
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.Ok(new { metadata = asset.Metadata, applicationOnly = true, legacyPortalCompatible = false });
        }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("The application-owned image could not be read.", statusCode: 503); }
    });
    app.MapGet("/api/application-assets/{parameterId}/content", async (
        string parameterId, HttpContext context, CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try
        {
            RollbackWriteTestGuard.EnsureAllowed(applicationWriteModeEnabled, authenticated, memberCode, connectionString);
            var asset = await assetStorage.ReadAsync(parameterId, cancellationToken);
            if (asset is null) return Results.NotFound();
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.CacheControl = "private, no-store";
            return Results.File(asset.Content, asset.Metadata.ContentType);
        }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("The application-owned image could not be read.", statusCode: 503); }
    });
    app.MapGet("/api/application-assets", async (HttpContext context, CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try
        {
            RollbackWriteTestGuard.EnsureAllowed(applicationWriteModeEnabled, authenticated, memberCode, connectionString);
            return Results.Ok(new { assets = await assetStorage.ListAsync(cancellationToken), applicationOnly = true, legacyPortalCompatible = false });
        }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("Application-owned images could not be listed.", statusCode: 503); }
    });
    app.MapDelete("/api/application-assets/{parameterId}", async (
        string parameterId, HttpContext context, CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try
        {
            RollbackWriteTestGuard.EnsureAllowed(applicationWriteModeEnabled, authenticated, memberCode, connectionString);
            return Results.Ok(new { deleted = await assetStorage.DeleteAsync(parameterId, cancellationToken), applicationOnly = true });
        }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("The application-owned image could not be deleted.", statusCode: 503); }
    });

}

if (writeTestModeEnabled)
{
    var rollbackTestService = new SqlRollbackOnlySettingsTestService(catalog, connectionString, memberCode, writeTestModeEnabled);
    app.MapPost("/api/settings/test/rollback-only", async (
        RollbackOnlySettingsTestRequest request,
        HttpContext context,
        CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try { return Results.Ok(await rollbackTestService.RunAsync(request, authenticated, cancellationToken)); }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("The rollback-only settings test failed.", statusCode: 503); }
    });

}

if (applicationWriteModeEnabled)
{
    var persistentTestService = new SqlPersistentSettingsTestService(catalog, connectionString, memberCode, applicationWriteModeEnabled);
    var extensionSettingsService = new SqlExtensionSettingsPersistence(catalog, connectionString, memberCode, applicationWriteModeEnabled);
    // The application save route is available only through an explicit process setting in
    // Development/SqlServer mode. The service enforces the fixed allowlist and exact target.
    app.MapPut("/api/settings", async (
        UpdateRequest request,
        HttpContext context,
        CancellationToken cancellationToken) =>
    {
        var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
        try
        {
            var changes = request.Changes ?? [];
            var hasExtension = changes.Any(change => SqlExtensionSettingsPersistence.IsSupportedWrite(change.ParameterId));
            if (hasExtension && changes.Any(change => !SqlExtensionSettingsPersistence.IsSupportedWrite(change.ParameterId)))
                return Results.BadRequest(new ApiError("ValidationFailed", "Save extension-backed settings separately from legacy-column settings."));
            PersistentSettingsSaveResult result;
            if (hasExtension)
            {
                var values = await extensionSettingsService.SaveAsync(changes, authenticated, cancellationToken);
                result = new PersistentSettingsSaveResult(null, null, true, Values: values);
            }
            else
            {
                result = await persistentTestService.SaveAsync(
                    new PersistentSettingsSaveRequest(null, null, Changes: changes),
                    authenticated,
                    cancellationToken);
            }
            return Results.Ok(result);
        }
        catch (SettingsException exception) { return SettingsError(exception); }
        catch (Exception) { return Results.Problem("The isolated settings save could not be verified.", statusCode: 503); }
    });

    if (writeTestModeEnabled)
    {
        app.MapPost("/api/settings/test/persistent-save", async (
            PersistentSettingsSaveRequest request,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var authenticated = context.Items["B2BStoreAdminAuthorized"] is true;
            try
            {
                var result = await persistentTestService.SaveAsync(request, authenticated, cancellationToken);
                return Results.Ok(result);
            }
            catch (SettingsException exception) { return SettingsError(exception); }
            catch (Exception) { return Results.Problem("The isolated settings save could not be verified.", statusCode: 503); }
        });
    }
}

app.Run();

static bool FixedTimeEquals(string expected, string actual)
{
    var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
    var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
    return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
}

static IResult SettingsError(SettingsException exception)
{
    var status = exception.Code switch
    {
        "ValidationFailed" => StatusCodes.Status400BadRequest,
        "ReadOnlyMode" => StatusCodes.Status405MethodNotAllowed,
        "TestWriteUnauthorized" => StatusCodes.Status401Unauthorized,
        "TestWriteModeRequired" => StatusCodes.Status404NotFound,
        "DatabaseIsolationViolation" => StatusCodes.Status403Forbidden,
        "MappingNotVerified" or "AmbiguousMemberCode" or "ConcurrencyConflict" or "ReadbackMismatch" or "RollbackVerificationFailed" => StatusCodes.Status409Conflict,
        "MemberCodeMissing" or "DatabaseNotConfigured" => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status503ServiceUnavailable,
    };
    return Results.Json(new ApiError(exception.Code, exception.Message), statusCode: status);
}

public partial class Program { }
