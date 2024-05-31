using System.Reflection;

namespace WELearning.Core.FunctionBlocks.Abstracts;

public interface ITypeProvider
{
    IEnumerable<Assembly> GetAssemblies(IEnumerable<string> assemblies);
    IEnumerable<Type> GetTypes(IEnumerable<string> types);
}