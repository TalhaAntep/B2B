using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public class BitmaskEditorTests
{
    private static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal) { "8192", "4611686018427387904" };

    [Fact]
    public void EnablingFlagPreservesNeighborAndUnknownBits()
    {
        var unknown = 1L << 50;
        var neighbor = 1L << 40;
        var result = BitmaskEditor.Apply(neighbor | unknown, [new BitOperation("8192", true)], Allowed);
        Assert.Equal(neighbor | unknown | 8192L, result);
    }

    [Fact]
    public void DisablingFlagPreservesNeighborAndUnknownBits()
    {
        var unknown = 1L << 50;
        var neighbor = 1L << 40;
        var result = BitmaskEditor.Apply(neighbor | unknown | 8192L, [new BitOperation("8192", false)], Allowed);
        Assert.Equal(neighbor | unknown, result);
    }

    [Fact]
    public void ReapplyingAnOperationIsIdempotent()
    {
        var once = BitmaskEditor.Apply(4, [new BitOperation("8192", true)], Allowed);
        var twice = BitmaskEditor.Apply(once, [new BitOperation("8192", true)], Allowed);
        Assert.Equal(once, twice);
    }

    [Fact]
    public void NullRemainsUnsetUntilAUserEditsTheMask()
    {
        Assert.Null(SettingUpdateValidator.ReadExpected(System.Text.Json.JsonDocument.Parse("null").RootElement));
        Assert.NotEqual("0", (string?)null);
        Assert.Equal(8192L, BitmaskEditor.Apply(null, [new BitOperation("8192", true)], Allowed));
    }

    [Fact]
    public void HighOrderBitIsExactAndOutOfCatalogBitsAreRejected()
    {
        const string highBit = "4611686018427387904";
        var result = BitmaskEditor.Apply(0, [new BitOperation(highBit, true)], Allowed);
        Assert.Equal(1L << 62, result);
        Assert.Throws<SettingsException>(() => BitmaskEditor.Apply(0, [new BitOperation("16384", true)], Allowed));
    }
}
