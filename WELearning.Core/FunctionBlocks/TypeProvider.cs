
using System.Collections.Concurrent;
using System.Reflection;
using WELearning.Core.FunctionBlocks.Abstracts;

namespace WELearning.Core.FunctionBlocks;

public class TypeProvider : ITypeProvider
{
    public readonly ConcurrentDictionary<string, Type> _typeMap;
    public readonly ConcurrentDictionary<string, Assembly> _assemblyMap;

    public TypeProvider()
    {
        _typeMap = new ConcurrentDictionary<string, Type>();
        _assemblyMap = new ConcurrentDictionary<string, Assembly>();
    }

    public IEnumerable<Assembly> GetAssemblies(IEnumerable<string> assemblies)
        => assemblies.Select(a => _assemblyMap.GetOrAdd(a, (key) => Assembly.Load(key)));

    public IEnumerable<Type> GetTypes(IEnumerable<string> types)
        => types.Select(type => _typeMap.GetOrAdd(type, (key) => Type.GetType(key)));
}