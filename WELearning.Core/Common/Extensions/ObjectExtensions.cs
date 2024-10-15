using System.Globalization;
using WELearning.Core.Constants;

namespace WELearning.Core.Common.Extensions;

public static class ObjectExtensions
{
    public static double? AsDouble(this object raw)
    {
        if (raw is double dValue)
            return dValue;
        var value = raw?.ToString();
        if (string.IsNullOrEmpty(value))
            return null;
        return double.Parse(value);
    }

    public static int? AsInt(this object raw)
    {
        if (raw is int iValue)
            return iValue;
        var value = raw?.ToString();
        if (string.IsNullOrEmpty(value))
            return null;
        return int.Parse(value);
    }

    public static bool? AsBool(this object raw)
    {
        if (raw is bool bValue)
            return bValue;
        var value = raw?.ToString();
        if (string.IsNullOrEmpty(value))
            return null;
        return value switch
        {
            "1" => true,
            "0" => false,
            _ => (bool?)bool.Parse(value),
        };
    }

    public static DateTime? AsDateTime(this object raw)
    {
        if (raw is DateTime dValue)
            return dValue;

        var value = raw?.ToString();
        if (string.IsNullOrEmpty(value))
            return null;

        var dateTime = DateTime.Parse(value, provider: CultureInfo.InvariantCulture, styles: DateTimeStyles.RoundtripKind);
        if (dateTime.Kind == DateTimeKind.Unspecified)
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        return dateTime;
    }

    public static bool As(this object raw, EDataType dataType, out object result)
    {
        result = raw;
        try
        {
            result = raw.As(dataType);
            return true;
        }
        catch { return false; }
    }

    public static object As(this object raw, EDataType dataType)
    {
        if (raw is null)
            return raw;
        return dataType switch
        {
            EDataType.Bool => raw.AsBool(),
            EDataType.Int => raw.AsInt(),
            EDataType.DateTime => raw.AsDateTime(),
            EDataType.Double => raw.AsDouble(),
            EDataType.Numeric => raw.AsDouble(),
            EDataType.String => raw.ToString(),
            EDataType.Object => raw,
            EDataType.Reference => raw,
            _ => throw new NotSupportedException($"Data type {dataType} is not supported for this value!"),
        };
    }
}
