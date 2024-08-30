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
        if (value is null)
            return null;
        return double.Parse(value);
    }

    public static int? AsInt(this object raw)
    {
        if (raw is int iValue)
            return iValue;
        var value = raw?.ToString();
        if (value is null)
            return null;
        return int.Parse(value);
    }

    public static bool? AsBool(this object raw)
    {
        if (raw is bool bValue)
            return bValue;
        var value = raw?.ToString();
        if (value is null)
            return null;
        return bool.Parse(value);
    }

    public static DateTime? AsDateTime(this object raw)
    {
        if (raw is DateTime dValue)
            return dValue;
        var value = raw?.ToString();
        if (value is null)
            return null;
        return DateTime.Parse(value, provider: CultureInfo.InvariantCulture);
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
            EDataType.Object => raw,
            EDataType.String => raw.ToString(),
            _ => throw new NotSupportedException($"Data type {dataType} is not supported for this value!"),
        };
    }
}