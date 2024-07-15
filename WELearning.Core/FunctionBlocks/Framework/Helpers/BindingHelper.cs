using WELearning.Core.FunctionBlocks.Framework.Abstracts;

namespace WELearning.Core.FunctionBlocks.Framework.Helpers;

public static class BindingHelper
{
    public static object GetValue(object rawValue)
    {
        if (rawValue is IReadBinding readBinding)
            return readBinding.Value;
        return rawValue;
    }
}