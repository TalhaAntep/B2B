using Microsoft.Data.SqlClient;

namespace B2BStore.Settings.Api;

public static class HostedWriteGuard
{
    public const string Database = "db82141";
    public const string Member = "store5";

    public static void EnsureAllowed(bool enabled, bool authenticated, string? memberCode, string? connectionString)
    {
        if (!authenticated) throw new SettingsException("TestWriteUnauthorized", "An administrator token is required.");
        if (!enabled) throw new SettingsException("TestWriteModeRequired", "Hosted writes are disabled.");
        if (!string.Equals(memberCode, Member, StringComparison.Ordinal))
            throw new SettingsException("DatabaseIsolationViolation", "Unexpected member code.");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new SettingsException("DatabaseNotConfigured", "SQL connection is missing.");
        SqlConnectionStringBuilder parsed;
        try { parsed = new SqlConnectionStringBuilder(connectionString); }
        catch (ArgumentException) { throw new SettingsException("DatabaseIsolationViolation", "Invalid SQL connection."); }
        if (!string.Equals(parsed.InitialCatalog, Database, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parsed.DataSource))
            throw new SettingsException("DatabaseIsolationViolation", "SQL target does not match the hosted database.");
    }
}
