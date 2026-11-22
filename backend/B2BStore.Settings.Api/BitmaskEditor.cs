using System.Globalization;
using System.Numerics;

namespace B2BStore.Settings.Api;

public static class BitmaskEditor
{
    public static long Apply(long? currentValue, IReadOnlyList<BitOperation> operations, IReadOnlySet<string> allowedValues)
    {
        var current = new BigInteger(currentValue ?? 0L);
        foreach (var operation in operations)
        {
            if (!allowedValues.Contains(operation.OptionValue)
                || !BigInteger.TryParse(operation.OptionValue, NumberStyles.None, CultureInfo.InvariantCulture, out var mask)
                || mask <= BigInteger.Zero
                || (mask & (mask - BigInteger.One)) != BigInteger.Zero
                || mask > long.MaxValue)
            {
                throw new SettingsException("ValidationFailed", "A bit option is not present in the allowed catalog.");
            }
            current = operation.Enabled ? current | mask : current & ~mask;
        }
        if (current < long.MinValue || current > long.MaxValue)
            throw new SettingsException("ValidationFailed", "The resulting bitmask is outside the SQL bigint range.");
        return (long)current;
    }
}
