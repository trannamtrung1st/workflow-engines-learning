namespace WELearning.Core.Reflection.Extensions;

public static class ObjectExtensions
{
    // Reference: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    public static bool IsNumeric(this object value)
    {
        return value != null && (
            value is sbyte || value is byte || value is short || value is ushort
            || value is int || value is uint || value is long || value is ulong
            || value is nint || value is nuint
            || value is float || value is double || value is decimal
        );
    }

    public static bool Is<T>(this object value) => value is T;
}