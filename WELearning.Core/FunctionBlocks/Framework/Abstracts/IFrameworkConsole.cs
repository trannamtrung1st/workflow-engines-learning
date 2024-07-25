namespace WELearning.Core.FunctionBlocks.Framework.Abstracts;

public interface IFrameworkConsole
{
    void Trace(params object[] data);
    void Debug(params object[] data);
    void Log(params object[] data);
    void Error(params object[] data);
    void Warn(params object[] data);
}
