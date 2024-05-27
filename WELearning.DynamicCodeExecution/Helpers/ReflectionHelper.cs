using System.Reflection;

namespace WELearning.DynamicCodeExecution.Helpers;

public static class ReflectionHelper
{
    public static Assembly[] CombineAssemblies(IEnumerable<Assembly> assemblies, IEnumerable<Type> types)
    {
        if (assemblies == null && types == null) return null;
        var combinedList = assemblies ?? Array.Empty<Assembly>();
        if (types?.Any() == true) combinedList = combinedList.Concat(types.Select(t => t.Assembly));
        return combinedList.ToArray();
    }
    public static Type[] CombineTypes(IEnumerable<Assembly> assemblies, IEnumerable<Type> types)
    {
        if (assemblies == null && types == null) return null;
        var combinedList = types ?? Array.Empty<Type>();
        if (assemblies?.Any() == true) combinedList = combinedList.Concat(assemblies.SelectMany(t => t.ExportedTypes));
        return combinedList.ToArray();
    }
}