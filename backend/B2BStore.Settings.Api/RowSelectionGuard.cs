namespace B2BStore.Settings.Api;

public static class RowSelectionGuard
{
    public static void EnsureSingleTarget(long count)
    {
        if (count == 0) throw new SettingsException("MemberCodeMissing", "The configured member context matched no DealerSettings row.");
        if (count > 1) throw new SettingsException("AmbiguousMemberCode", "The configured member context matched multiple DealerSettings rows; reads and writes are blocked.");
    }
}
