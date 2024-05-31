using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Extensions;

public static class EnumExtensions
{
    public static EVariableType ToVariableType(this EBindingType bindingType)
        => (EVariableType)(int)bindingType;
}
