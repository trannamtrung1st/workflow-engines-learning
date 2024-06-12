using WELearning.ConsoleApp.Testing.Framework;
using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Samples.DeviceService.FunctionBlock.Composites;
using WELearning.Samples.DeviceService.FunctionBlock.ValueObjects;
using WELearning.Samples.DeviceService.Models;
using WELearning.Samples.DeviceService.Persistent;
using WELearning.Samples.DeviceService.Services.Abstracts;
using WELearning.Shared.Concurrency.Abstracts;

namespace WELearning.Samples.DeviceService.Services;

public class FunctionBlockService : IFunctionBlockService
{
    private readonly IConfiguration _configuration;
    private readonly IBlockRunner _blockRunner;
    private readonly IFunctionRunner _functionRunner;
    private readonly IBlockFrameworkFactory _blockFrameworkFactory;
    private readonly DeviceFunctionFramework _functionFramework;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly IAssetService _assetService;
    private readonly DataStore _dataStore;
    private readonly IMonitoring _monitoring;
    private readonly ILogger<IExecutionControl> _controlLogger;

    public FunctionBlockService(
        IConfiguration configuration,
        IBlockRunner blockRunner,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        DeviceFunctionFramework functionFramework,
        IAssetService assetService,
        DataStore dataStore,
        IMonitoring monitoring,
        ILogger<IExecutionControl> controlLogger,
        ISyncAsyncTaskRunner taskRunner)
    {
        _configuration = configuration;
        _blockRunner = blockRunner;
        _functionRunner = functionRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
        _functionFramework = functionFramework;
        _assetService = assetService;
        _dataStore = dataStore;
        _controlLogger = controlLogger;
        _monitoring = monitoring;
        _taskRunner = taskRunner;
    }

    public async Task HandleAttributeChanged(AttributeChangedEvent @event, CancellationToken cancellationToken)
    {
        const string MonitoringCategory = "Consuming rate";
        // [NOTE] demo only
        if (@event.AttributeName != "dynamic1")
            return;

        var fbTimeout = _configuration.GetValue<TimeSpan>("FunctionBlock:Timeout");
        using var runTokens = new RunTokens(timeout: fbTimeout, termination: cancellationToken);

        var cfbDef = await BuildBlock();
        using var execControl = new CompositeEC<DeviceFunctionFramework>(
            block: new(cfbDef.Id), definition: cfbDef,
            _blockRunner, _functionRunner, _blockFrameworkFactory, _functionFramework, _taskRunner);
        execControl.Running += (o, e) => execControl.LogBlockActivity(logger: _controlLogger);
        execControl.Completed += (o, e) => execControl.LogBlockActivity(logger: _controlLogger);
        execControl.Failed += (o, e) => execControl.LogBlockActivity(logger: _controlLogger);
        execControl.ControlRunning += (o, e) => (o as IExecutionControl)?.LogBlockActivity(logger: _controlLogger);
        execControl.ControlCompleted += (o, e) => (o as IExecutionControl)?.LogBlockActivity(logger: _controlLogger);
        execControl.ControlFailed += (o, e) => (o as IExecutionControl)?.LogBlockActivity(logger: _controlLogger);

        var bindings = await PrepareBindings(@event, execControl);
        var runRequest = new RunBlockRequest(bindings, runTokens);
        try
        {
            await _blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
            await RecordOutputs(execControl);
            _monitoring.Capture(MonitoringCategory, count: 1);
        }
        catch
        {
            // [NOTE] retry
        }
    }

    private async Task<CompositeBlockDef> BuildBlock()
    {
        var cfbDef = await _dataStore.GetCfbDefinition(SumAttributesCFB.CfbId);

        var usingBfbDefIds = cfbDef.Blocks.Select(b => b.DefinitionId);
        var usingBfbDefs = await _dataStore.GetBfbDefinitions(usingBfbDefIds);

        var importBfbDefIds = usingBfbDefs
            .Where(b => b.ImportBlockIds?.Any() == true)
            .SelectMany(b => b.ImportBlockIds);
        var importBfbDefs = await _dataStore.GetBfbDefinitions(importBfbDefIds);

        cfbDef.MapDefinitions(usingBfbDefs.Concat(importBfbDefs));
        return cfbDef;
    }

    private async Task RecordOutputs(IExecutionControl execControl)
    {
        var sum = execControl.GetOutput("AttrSum") as AttributeValueObject;
        var prevSum = execControl.GetOutput("AttrPrevSum") as AttributeValueObject;
        await _assetService.UpdateRuntime(new[] { sum.Snapshot, prevSum.Snapshot });
    }

    private async Task<IEnumerable<VariableBinding>> PrepareBindings(AttributeChangedEvent trigger, IExecutionControl execControl)
    {
        var assetId = trigger.AssetId;
        var snapshots = (await _assetService.GetSnapshots(new[]
        {
            (assetId, "dynamic1"),
            (assetId, "dynamic2"),
            (assetId, "sum"),
            (assetId, "prevSum")
        })).ToDictionary(a => a.AttributeName);

        var iAttr1 = execControl.GetVariable("Attr1", EVariableType.Input);
        var iAttr2 = execControl.GetVariable("Attr2", EVariableType.Input);
        var oAttr1 = execControl.GetVariable("AttrSum", EVariableType.Output);
        var oAttr2 = execControl.GetVariable("AttrPrevSum", EVariableType.Output);
        var iAttr1Ref = new AttributeValueObject(iAttr1, snapshots["dynamic1"]);
        var iAttr2Ref = new AttributeValueObject(iAttr2, snapshots["dynamic2"]);
        var oAttr1Ref = new AttributeValueObject(oAttr1, snapshots["sum"]);
        var oAttr2Ref = new AttributeValueObject(oAttr2, snapshots["prevSum"]);

        var bindings = new HashSet<VariableBinding>();
        bindings.Add(new(variableName: iAttr1.Name, reference: iAttr1Ref, type: EBindingType.Input));
        bindings.Add(new(variableName: iAttr2.Name, reference: iAttr2Ref, type: EBindingType.Input));
        bindings.Add(new(variableName: oAttr1.Name, reference: oAttr1Ref, type: EBindingType.Output));
        bindings.Add(new(variableName: oAttr2.Name, reference: oAttr2Ref, type: EBindingType.Output));

        return bindings;
    }
}