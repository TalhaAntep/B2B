using System.Globalization;

namespace B2BStore.Settings.Api;

/// <summary>Preserves the 16-bit pattern stored by SQL smallint while exposing unsigned decimal masks.</summary>
public static class SmallintBitmaskCodec
{
    public static short Encode(ulong value)
    {
        if (value > ushort.MaxValue) throw new SettingsException("ValidationFailed", "The mask exceeds the SQL smallint bit width.");
        return unchecked((short)(ushort)value);
    }

    public static ushort Decode(short value) => unchecked((ushort)value);

    public static (short LowWord, long HighMask) SplitProductSearch(long value)
    {
        if (value < 0) throw new SettingsException("ValidationFailed", "The product search mask must be a non-negative decimal integer.");
        return (Encode((ulong)value & ushort.MaxValue), value & ~((long)ushort.MaxValue));
    }

    public static string? CombineProductSearch(short? lowWord, string? highMask)
    {
        if (lowWord is null && highMask is null) return null;
        var low = lowWord is null ? 0UL : Decode(lowWord.Value);
        var high = highMask is null ? 0UL : ulong.Parse(highMask, NumberStyles.None, CultureInfo.InvariantCulture);
        if ((high & ushort.MaxValue) != 0)
            throw new SettingsException("DatabaseIsolationViolation", "The product search high mask overlaps its legacy smallint word.");
        return (low | high).ToString(CultureInfo.InvariantCulture);
    }
}
