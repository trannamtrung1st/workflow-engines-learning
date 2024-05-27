using System.Collections.Concurrent;
using WELearning.Core.FunctionBlocks.Constants;

namespace WELearning.Core.FunctionBlocks.Models.Runtime;

public class BlockExecutionControl
{
    private readonly ConcurrentDictionary<string, ValueObject> _inputSnapshot;
    private readonly ConcurrentDictionary<string, ValueObject> _outputSnapshot;
    private readonly ConcurrentDictionary<string, ValueObject> _internalDataSnapshot;
    public BlockExecutionControl(string blockId, string initialState)
    {
        _inputSnapshot = new();
        _outputSnapshot = new();
        _internalDataSnapshot = new();
        BlockId = blockId;
        CurrentState = initialState;
    }

    public string BlockId { get; set; }
    public virtual string CurrentState { get; protected internal set; }
    public virtual Exception Exception { get; protected internal set; }
    public virtual EBlockExecutionStatus Status { get; protected internal set; }

    public virtual ValueObject GetInput(string key) => GetValueObject(_inputSnapshot, key);
    public virtual ValueObject GetOutput(string key) => GetValueObject(_outputSnapshot, key);
    public virtual ValueObject GetInternalData(string key) => GetValueObject(_internalDataSnapshot, key);
    private ValueObject GetValueObject(ConcurrentDictionary<string, ValueObject> source, string key)
        => source.GetOrAdd(key, (key) => new ValueObject());
}
