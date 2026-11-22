using System.Text.Json;
using B2BStore.Settings.Api;

namespace B2BStore.Settings.Tests;

public sealed class SpecialSettingsPersistenceTests
{
    private static ParameterCatalog Catalog => ParameterCatalog.Load(Path.Combine(AppContext.BaseDirectory, "parameter-catalog.json"));

    [Theory]
    [InlineData(512, 512)]
    [InlineData(32768, -32768)]
    [InlineData(33280, -32256)]
    [InlineData(65535, -1)]
    public void ParameterPaymentRoundTripsTheUnsignedSmallintBitPattern(uint apiMask, short sqlValue)
    {
        var encoded = DealerSettingValueCodec.ToDatabaseValue(DealerSettingValueCodec.ParameterPaymentParameterId, apiMask.ToString());
        Assert.Equal(sqlValue, encoded);
        Assert.Equal(apiMask.ToString(), DealerSettingValueCodec.ToApiValue(DealerSettingValueCodec.ParameterPaymentParameterId, encoded));
    }

    [Fact]
    public void ParameterPaymentRejectsValuesOutsideTheSixteenBitDomain()
    {
        var error = Assert.Throws<SettingsException>(() => DealerSettingValueCodec.ToDatabaseValue(
            DealerSettingValueCodec.ParameterPaymentParameterId, "65536"));
        Assert.Equal("ValidationFailed", error.Code);
    }

    [Fact]
    public void ParameterPaymentChangesPreserveUnlistedBitSixteenAndUnknownBits()
    {
        var allowed = Catalog.AllSettings.Single(item => item.ParameterId == DealerSettingValueCodec.ParameterPaymentParameterId)
            .Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
        var baseline = 512L | 16L | 1L << 14;
        var changed = BitmaskEditor.Apply(baseline, [new BitOperation("32768", true), new BitOperation("512", false)], allowed);
        Assert.Equal((baseline & ~512L) | 32768L, changed);
        Assert.Equal((short)-16368, DealerSettingValueCodec.ToDatabaseValue(DealerSettingValueCodec.ParameterPaymentParameterId, changed.ToString()));
    }

    [Fact]
    public void ParameterPaymentConcurrencyExpectedValueAcceptsUnsignedDecimalMasksAboveSignedSmallint()
    {
        var definition = Catalog.AllSettings.Single(item => item.ParameterId == DealerSettingValueCodec.ParameterPaymentParameterId);
        var change = new SettingChange(definition.ParameterId,
            JsonDocument.Parse("\"33280\"").RootElement.Clone(), JsonDocument.Parse("null").RootElement.Clone(),
            [new BitOperation("32768", false)]);
        SqlPersistentSettingsTestService.ValidateCatalogChange(definition, "smallint", true, true, change);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(8192)]
    [InlineData(32768)]
    [InlineData(65536)]
    [InlineData(524288)]
    [InlineData(1048576)]
    public void ProductSearchSplitAndMergeRetainsEachSourceFlag(long flag)
    {
        var (low, high) = SmallintBitmaskCodec.SplitProductSearch(flag);
        Assert.Equal(flag.ToString(), SmallintBitmaskCodec.CombineProductSearch(low, high.ToString()));
        Assert.Equal(flag & ushort.MaxValue, SmallintBitmaskCodec.Decode(low));
    }

    [Fact]
    public void ProductSearchSplitAndMergePreservesUnknownBitsAndMaximumSourceMask()
    {
        const long maximumSourceMask = 2_097_151;
        const long unknownBit = 1L << 25;
        var combined = maximumSourceMask | unknownBit;
        var (low, high) = SmallintBitmaskCodec.SplitProductSearch(combined);
        Assert.Equal(combined.ToString(), SmallintBitmaskCodec.CombineProductSearch(low, high.ToString()));
        var sourceOptions = Catalog.AllSettings.Single(item => item.ParameterId == DealerSettingValueCodec.ParameterProductSearchParameterId)
            .Options.Select(option => option.Value).ToHashSet(StringComparer.Ordinal);
        var updated = BitmaskEditor.Apply(combined, [new BitOperation("1", false)], sourceOptions);
        Assert.Equal(combined & ~1L, updated);
        Assert.NotEqual(0, updated & unknownBit);
        var splitUpdated = SmallintBitmaskCodec.SplitProductSearch(updated);
        Assert.Equal(updated.ToString(), SmallintBitmaskCodec.CombineProductSearch(splitUpdated.LowWord, splitUpdated.HighMask.ToString()));
    }

    [Fact]
    public void ProductSearchHighMaskCannotOverlapTheLegacyLowWord()
    {
        var error = Assert.Throws<SettingsException>(() => SmallintBitmaskCodec.CombineProductSearch((short)1, "1"));
        Assert.Equal("DatabaseIsolationViolation", error.Code);
    }

    [Theory]
    [InlineData(SqlExtensionSettingsPersistence.ProductDetailNotLogin2Id, "4611686018427387904")]
    [InlineData(SqlExtensionSettingsPersistence.ParameterMenu2Id, "1")]
    public void ExtensionBitmasksRemainExactDecimalStrings(string parameterId, string value)
    {
        var stored = SqlExtensionSettingsPersistence.EncodeApplicationExtensionValue(Catalog, parameterId, value);
        Assert.Equal(value, stored);
        Assert.Equal(value, SqlExtensionSettingsPersistence.DecodeApplicationExtensionValue(Catalog, parameterId, stored));
    }

    [Fact]
    public void MissingAndExplicitZeroOrFalseExtensionValuesRemainDistinct()
    {
        Assert.Null(SqlExtensionSettingsPersistence.DecodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.ParameterMenu2Id, null));
        Assert.Equal("0", SqlExtensionSettingsPersistence.DecodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.ParameterMenu2Id, "0"));
        Assert.Null(SqlExtensionSettingsPersistence.DecodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.WhatsAppOrderNotificationId, null));
        Assert.Equal(false, SqlExtensionSettingsPersistence.DecodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.WhatsAppOrderNotificationId, "false"));
    }

    [Fact]
    public void HorizontalAndVerticalFavoritePositionsHaveIndependentCanonicalValues()
    {
        Assert.Equal("Left", SqlExtensionSettingsPersistence.EncodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.FavoriteIconPositionId, "rbFavoriteIconLeft"));
        Assert.Equal("Right", SqlExtensionSettingsPersistence.EncodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.FavoriteIconPositionId, "rbFavoriteIconRight"));
        Assert.Equal("Top", SqlExtensionSettingsPersistence.EncodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.FavoriteIconVerticalPositionId, "rbFavoriteIconTop"));
        Assert.Equal("Bottom", SqlExtensionSettingsPersistence.EncodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.FavoriteIconVerticalPositionId, "rbFavoriteIconBottom"));
        Assert.Throws<SettingsException>(() => SqlExtensionSettingsPersistence.EncodeApplicationExtensionValue(Catalog,
            SqlExtensionSettingsPersistence.FavoriteIconVerticalPositionId, "rbFavoriteIconLeft"));
    }

    [Fact]
    public async Task ExtensionWritesRequireAuthenticationAndExplicitIsolatedMode()
    {
        var change = new SettingChange(SqlExtensionSettingsPersistence.ParameterMenu2Id,
            JsonDocument.Parse("null").RootElement.Clone(), JsonDocument.Parse("null").RootElement.Clone(), []);
        var authenticated = new SqlExtensionSettingsPersistence(Catalog,
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Store5_Settings_Test;Integrated Security=True;TrustServerCertificate=True",
            "store5", true);
        var unauthorized = await Assert.ThrowsAsync<SettingsException>(() => authenticated.SaveAsync([change], false, CancellationToken.None));
        Assert.Equal("TestWriteUnauthorized", unauthorized.Code);
        var disabled = new SqlExtensionSettingsPersistence(Catalog,
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Store5_Settings_Test;Integrated Security=True;TrustServerCertificate=True",
            "store5", false);
        var mode = await Assert.ThrowsAsync<SettingsException>(() => disabled.SaveAsync([change], true, CancellationToken.None));
        Assert.Equal("TestWriteModeRequired", mode.Code);
    }

    [Fact]
    public void ExtensionAllowlistRejectsArbitraryKeys()
    {
        Assert.True(SqlExtensionSettingsPersistence.IsSupportedWrite(SqlExtensionSettingsPersistence.ParameterMenu2Id));
        Assert.True(SqlExtensionSettingsPersistence.IsSupportedWrite(SqlExtensionSettingsPersistence.ProductSearchId));
        Assert.False(SqlExtensionSettingsPersistence.IsSupportedWrite("dbo.DealerSettings.ParameterMenu"));
    }
}
