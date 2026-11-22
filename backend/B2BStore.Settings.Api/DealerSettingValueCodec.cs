namespace B2BStore.Settings.Api;

/// <summary>Field-specific wire/database value conversions supported by the supplied control and row evidence.</summary>
public static class DealerSettingValueCodec
{
    public const string DiscountTypeParameterId = "ctl00_PageContent_ddlDiscountType";
    public const string UseFilteringParameterId = "ctl00_PageContent_ddUseFiltering";
    public const string ParameterPaymentParameterId = "ctl00_PageContent_ddlPaymentParameter";
    public const string ParameterProductSearchParameterId = "ctl00_PageContent_ddlProductSearchParameter";

    public static object? ToApiValue(string parameterId, object? databaseValue)
    {
        if (parameterId is ParameterPaymentParameterId or ParameterProductSearchParameterId && databaseValue is not null)
        {
            var signed = Convert.ToInt16(databaseValue, System.Globalization.CultureInfo.InvariantCulture);
            return SmallintBitmaskCodec.Decode(signed).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (parameterId != DiscountTypeParameterId || databaseValue is null) return databaseValue;
        var stored = Convert.ToString(databaseValue, System.Globalization.CultureInfo.InvariantCulture);
        return stored is "A" or "P" or "C" ? $"'{stored}'" : stored;
    }

    public static object? ToDatabaseValue(string parameterId, object? apiValue)
    {
        if (parameterId is ParameterPaymentParameterId or ParameterProductSearchParameterId && apiValue is not null)
        {
            var unsigned = Convert.ToUInt64(apiValue, System.Globalization.CultureInfo.InvariantCulture);
            if (unsigned > ushort.MaxValue)
                throw new SettingsException("ValidationFailed", "The smallint-backed setting exceeds its 16-bit storage range.");
            return SmallintBitmaskCodec.Encode(unsigned);
        }
        if (parameterId != DiscountTypeParameterId || apiValue is null) return apiValue;
        if (apiValue is not string option || option.Length != 3 || option[0] != '\'' || option[2] != '\''
            || option[1] is not ('A' or 'P' or 'C'))
            throw new SettingsException("ValidationFailed", "DiscountType must be one of its source option codes.");
        return option[1].ToString();
    }
}
