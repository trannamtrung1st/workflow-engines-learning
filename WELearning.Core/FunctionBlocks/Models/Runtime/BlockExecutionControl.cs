using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockExecutionControl
{
    public BlockExecutionControl(string blockId, string initialState)
    {
        BlockId = blockId;
        CurrentState = initialState;
        InputSnapshot = new ConcurrentDictionary<string, object>();
        OutputSnapshot = new ConcurrentDictionary<string, object>();
    }

    public string BlockId { get; set; }
    public virtual string CurrentState { get; protected internal set; }
    public virtual ConcurrentDictionary<string, object> InputSnapshot { get; }
    public virtual ConcurrentDictionary<string, object> OutputSnapshot { get; }
    public virtual Exception Exception { get; protected internal set; }
    public virtual EBlockExecutionStatus Status { get; protected internal set; }
}