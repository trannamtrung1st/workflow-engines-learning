using WELearning.Core.FunctionBlocks;
using WELearning.Core.FunctionBlocks.Abstracts;
using WELearning.Core.FunctionBlocks.Constants;
using WELearning.Core.FunctionBlocks.Exceptions;
using WELearning.Core.FunctionBlocks.Framework.Abstracts;
using WELearning.Core.FunctionBlocks.Models.Design;
using WELearning.Core.FunctionBlocks.Models.Runtime;
using WELearning.DynamicCodeExecution.Models;
using WELearning.Samples.FBWorker.FunctionBlock;
using WELearning.Samples.FBWorker.FunctionBlock.ValueObjects;
using WELearning.Samples.FBWorker.Services.Abstracts;
using WELearning.Samples.Shared.Models;
using TNT.Boilerplates.Concurrency.Abstracts;
using TNT.Boilerplates.Diagnostic.Abstracts;
using WELearning.Samples.FBWorker.Constants;

namespace WELearning.Samples.FBWorker.Services;

public class FunctionBlockService : IFunctionBlockService
{
    private readonly IConfiguration _configuration;
    private readonly IBlockRunner _blockRunner;
    private readonly IFunctionRunner _functionRunner;
    private readonly IBlockFrameworkFactory _blockFrameworkFactory;
    private readonly ISyncAsyncTaskRunner _taskRunner;
    private readonly ILimiterManager _limiterManager;
    private readonly IAssetService _assetService;
    private readonly IRateMonitor _rateMonitor;
    private readonly ILogger<IExecutionControl> _controlLogger;
    private readonly IHttpClients _clients;
    private readonly IFunctionFrameworkFactory<DeviceFunctionFramework> _functionFrameworkFactory;

    public FunctionBlockService(
        IConfiguration configuration,
        IBlockRunner blockRunner,
        IFunctionRunner functionRunner,
        IBlockFrameworkFactory blockFrameworkFactory,
        IAssetService assetService,
        IRateMonitor rateMonitor,
        ILogger<IExecutionControl> controlLogger,
        ISyncAsyncTaskRunner taskRunner,
        ILimiterManager limiterManager,
        IHttpClients clients,
        IFunctionFrameworkFactory<DeviceFunctionFramework> functionFrameworkFactory)
    {
        _configuration = configuration;
        _blockRunner = blockRunner;
        _functionRunner = functionRunner;
        _blockFrameworkFactory = blockFrameworkFactory;
        _assetService = assetService;
        _controlLogger = controlLogger;
        _rateMonitor = rateMonitor;
        _taskRunner = taskRunner;
        _limiterManager = limiterManager;
        _clients = clients;
        _functionFrameworkFactory = functionFrameworkFactory;
    }

    public async Task HandleAttributeChanged(AttributeChangedEvent @event, CancellationToken cancellationToken)
    {
        const string MonitoringCategory = "Consuming rate";
        // [NOTE] demo only
        if (@event.AttributeName != "dynamic1")
            return;

        var fbTimeout = _configuration.GetValue<TimeSpan>("FunctionBlock:Timeout");
        using var runTokens = new RunTokens(timeout: fbTimeout, termination: cancellationToken);

        var cfbDef = await BuildBlock(@event.DemoBlockId, cancellationToken: runTokens.Combined);
        _limiterManager.TryGetTaskLimiter(ConcurrencyConstants.LimiterNames.TaskLimiter, out var taskLimiter);
        using var execControl = new CompositeEC<DeviceFunctionFramework>(
            block: new(cfbDef.Id), definition: cfbDef,
            _blockRunner, _functionRunner, _blockFrameworkFactory, _functionFrameworkFactory, _taskRunner, taskLimiter);

        RegisterLogActivityHandlers(execControl);
        try
        {
            var bindings = await PrepareBindings(@event, execControl, cancellationToken: runTokens.Combined);
            var runRequest = new RunBlockRequest(bindings, runTokens);
            await _blockRunner.Run(runRequest, execControl, optimizationScopeId: default);
            await RecordOutputs(execControl, cancellationToken: runTokens.Combined);
        }
        catch (Exception ex)
        {
            if (ex is not FunctionRuntimeException runtimeEx || !runtimeEx.IsGracefulTerminated())
                throw;
            // [NOTE] retry
        }
        finally { UnregisterLogActivityHandlers(execControl); }
        _rateMonitor.Capture(MonitoringCategory, count: 1);
    }

    private void HandleLogActivity(object o, EventArgs e) => (o as IExecutionControl)?.LogBlockActivity(logger: _controlLogger);
    private void HandleLogActivity(object o, Exception e) => (o as IExecutionControl)?.LogBlockActivity(logger: _controlLogger);
    private void RegisterLogActivityHandlers(ICompositeEC execControl)
    {
        execControl.Running += HandleLogActivity;
        execControl.Completed += HandleLogActivity;
        execControl.Failed += HandleLogActivity;
        execControl.ControlRunning += HandleLogActivity;
        execControl.ControlCompleted += HandleLogActivity;
        execControl.ControlFailed += HandleLogActivity;
    }
    private void UnregisterLogActivityHandlers(ICompositeEC execControl)
    {
        execControl.Running -= HandleLogActivity;
        execControl.Completed -= HandleLogActivity;
        execControl.Failed -= HandleLogActivity;
        execControl.ControlRunning -= HandleLogActivity;
        execControl.ControlCompleted -= HandleLogActivity;
        execControl.ControlFailed -= HandleLogActivity;
    }

    private async Task<CompositeBlockDef> BuildBlock(string demoBlockId, CancellationToken cancellationToken)
    {
        var blockDefinitions = await _clients.Device.GetFromJsonAsync<BlockDefinitions>(
            requestUri: $"/api/fb/{demoBlockId}", cancellationToken);

        var cfbDef = blockDefinitions.Cfb;
        cfbDef.MapDefinitions(blockDefinitions.Bfbs);
        return cfbDef;
    }

    private async Task RecordOutputs(IExecutionControl execControl, CancellationToken cancellationToken)
    {
        var sum = execControl.GetOutput("AttrSum") as AttributeValueObject;
        var prevSum = execControl.GetOutput("AttrPrevSum") as AttributeValueObject;
        await _assetService.UpdateRuntime(new[] { sum.Snapshot, prevSum.Snapshot }, cancellationToken);
    }

    private async Task<IEnumerable<VariableBinding>> PrepareBindings(AttributeChangedEvent trigger, IExecutionControl execControl, CancellationToken cancellationToken)
    {
        var assetId = trigger.AssetId;
        var snapshots = (await _assetService.GetSnapshots(new[]
        {
            new[] { assetId, "dynamic1" },
            new[] { assetId, "dynamic2" },
            new[] { assetId, "sum" },
            new[] { assetId, "prevSum" }
        }, cancellationToken)).ToDictionary(a => a.AttributeName);

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