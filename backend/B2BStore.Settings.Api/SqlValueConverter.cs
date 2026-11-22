using System.Globalization;
using System.Data;
using Microsoft.Data.SqlClient;

namespace B2BStore.Settings.Api;

public static class SqlValueConverter
{
    public static SqlParameter CreateParameter(string name, string sqlType, object? value, byte precision = 0, byte scale = 0, int size = 0)
    {
        var type = sqlType.ToLowerInvariant();
        try
        {
            var sqlDbType = type switch
            {
                "bigint" => SqlDbType.BigInt,
                "int" => SqlDbType.Int,
                "smallint" => SqlDbType.SmallInt,
                "tinyint" => SqlDbType.TinyInt,
                "bit" => SqlDbType.Bit,
                "decimal" or "numeric" => SqlDbType.Decimal,
                "money" => SqlDbType.Money,
                "smallmoney" => SqlDbType.SmallMoney,
                "float" => SqlDbType.Float,
                "real" => SqlDbType.Real,
                "nvarchar" => SqlDbType.NVarChar,
                "varchar" => SqlDbType.VarChar,
                "nchar" => SqlDbType.NChar,
                "char" => SqlDbType.Char,
                "text" => SqlDbType.Text,
                "ntext" => SqlDbType.NText,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "date" => SqlDbType.Date,
                "datetime" => SqlDbType.DateTime,
                "datetime2" => SqlDbType.DateTime2,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "time" => SqlDbType.Time,
                _ => throw new SettingsException("ValidationFailed", "The verified setting uses an unsupported SQL data type."),
            };
            var parameter = new SqlParameter(name, sqlDbType) { Value = ConvertValue(type, value) ?? DBNull.Value };
            if (sqlDbType == SqlDbType.Decimal && precision > 0)
            {
                parameter.Precision = precision;
                parameter.Scale = scale;
            }
            if (size > 0 && sqlDbType is SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.NChar or SqlDbType.Char)
                parameter.Size = size;
            return parameter;
        }
        catch (SettingsException) { throw; }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentException)
        {
            throw new SettingsException("ValidationFailed", "The setting value does not match the verified SQL data type.");
        }
    }

    private static object? ConvertValue(string type, object? value)
    {
        if (value is null) return null;
        var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        return type switch
        {
            "bigint" => long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "int" => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "smallint" => short.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "tinyint" => byte.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
            "bit" => value is bool boolean ? boolean : bool.Parse(text),
            "decimal" or "numeric" or "money" or "smallmoney" => decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture),
            "float" => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            "real" => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
            "uniqueidentifier" => Guid.Parse(text),
            "date" or "datetime" or "datetime2" or "smalldatetime" => DateTime.Parse(text, CultureInfo.InvariantCulture),
            "datetimeoffset" => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture),
            "time" => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
            _ => value,
        };
    }
}
