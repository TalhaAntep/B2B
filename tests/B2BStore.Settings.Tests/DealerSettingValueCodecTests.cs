using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class DealerSettingValueCodecTests
{
    [Theory]
    [InlineData("'A'", "A")]
    [InlineData("'P'", "P")]
    [InlineData("'C'", "C")]
    public void DiscountTypeMapsQuotedHtmlValuesToUnquotedSqlCodes(string apiValue, string sqlValue)
    {
        Assert.Equal(sqlValue, DealerSettingValueCodec.ToDatabaseValue(DealerSettingValueCodec.DiscountTypeParameterId, apiValue));
        Assert.Equal(apiValue, DealerSettingValueCodec.ToApiValue(DealerSettingValueCodec.DiscountTypeParameterId, sqlValue));
    }

    [Theory]
    [InlineData("A")]
    [InlineData("'X'")]
    [InlineData("''")]
    public void DiscountTypeRejectsValuesOutsideTheSourceOptions(string value)
    {
        Assert.Throws<SettingsException>(() => DealerSettingValueCodec.ToDatabaseValue(DealerSettingValueCodec.DiscountTypeParameterId, value));
    }

    [Fact]
    public void UseFilteringNumericTextMaskPreservesUnknownAndUnselectedBits()
    {
        var options = new HashSet<string>(["2", "4", "8", "16"], StringComparer.Ordinal);
        var unknownBit = 1L << 45;
        var original = unknownBit | 2L | 8L;
        var next = BitmaskEditor.Apply(original, [new BitOperation("4", true)], options);

        Assert.Equal(unknownBit | 2L | 4L | 8L, next);
        Assert.Equal(original & ~30L, next & ~30L);
        Assert.Equal((original & 2L) != 0, (next & 2L) != 0);
        Assert.Equal((original & 8L) != 0, (next & 8L) != 0);
    }
}
