using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using ReactiveUI;
using VerifyPro.Enums;
using VerifyPro.Services;
using VerifyPro.Utils;

namespace VerifyPro.ViewModels;

public class MainTestViewModel : ReactiveObject
{   
    // 构造函数注入服务
    private readonly DetectionService _detectionService;
    private readonly DetectionStateService _stateService;
    private readonly ExportService _exportService;
    private CancellationTokenSource _doTestCts = new();
    private CancellationTokenSource _allTestCts = new();
    private DetectionState _currentState;
    public DetectionState CurrentState
    {
        get => _currentState;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentState, value);
            this.RaisePropertyChanged(nameof(StatusColor));
        }
    }
    private static readonly IBrush DarkOrangeBrush = Brush.Parse("#FF8C00");
    private static readonly IBrush DarkYellowBrush = Brush.Parse("#FBC02d");
    public IBrush StatusColor => CurrentState switch
    {
        DetectionState.Pass => Brushes.Green,
        DetectionState.Error => Brushes.Red,
        DetectionState.Idle => DarkOrangeBrush,
        DetectionState.Running => DarkYellowBrush,
        _ => Brushes.Gray
    };
    
    private ConfigFileViewModel ConfigFileVm { get; }
    public MainTestViewModel(DeviceCommManager commManager,DetectionService detectionService,DetectionStateService stateService, ExportService exportService, ConfigFileViewModel configFileVm)
    {
        _detectionService = detectionService;
        _stateService = stateService;
        _exportService = exportService;
        ConfigFileVm = configFileVm;
        _detectLog ="";
        
        StartTestCommand = ReactiveCommand.CreateFromTask(StartTestAsync);
        VoltageTestCommand = ReactiveCommand.CreateFromTask(VoltageTestAsync);
        CommTestCommand = ReactiveCommand.CreateFromTask(CommTestAsync);
        AiTestCommand = ReactiveCommand.CreateFromTask(AiTestAsync);
        DiTestCommand = ReactiveCommand.CreateFromTask(DiTestAsync);
        DoTestCommand = ReactiveCommand.CreateFromTask(DoTestAsync);
        ExportResultCommand = ReactiveCommand.Create(ExportResults);

        // 监听服务中的状态变化
        _stateService.StateChanged+= state =>
        {
            CurrentState = state;
        };

        // 初始化状态
        _currentState = _stateService.CurrentState;

        // 启动监控服务
        var switchInputMonitorService = new SwitchInputMonitorService(
            commManager,
            _stateService,
            StartTestAsync,
            CancelDoTest,
            AppendLog
        );

        switchInputMonitorService.Start();
        
        // 示例：监听 Config 是否变化
        this.WhenAnyValue(x => x.ConfigFileVm.Config)
            .Subscribe(config =>
            {
                Console.WriteLine("MainTestViewModel 收到配置：" + ConfigFileVm.Config?.Modelname);
                this.RaisePropertyChanged(nameof(ModelName));
            });
    }
    
    // 属性绑定到界面用于显示检测日志
    private string _detectLog;
    public string DetectLog
    {
        get => _detectLog;
        set => this.RaiseAndSetIfChanged(ref _detectLog, value);
    }
    public string? ModelName => ConfigFileVm.Config?.Modelname;
    public ReactiveCommand<Unit, Unit> StartTestCommand { get; }
    public ReactiveCommand<Unit, Unit> VoltageTestCommand { get; }
    public ReactiveCommand<Unit, Unit> CommTestCommand { get; }
    public ReactiveCommand<Unit, Unit> AiTestCommand { get; }
    public ReactiveCommand<Unit, Unit> DiTestCommand { get; }
    public ReactiveCommand<Unit, Unit> DoTestCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportResultCommand { get; }
    
    private async Task StartTestAsync()
    {
        if (!CanRunTest("All")) return;
        
        // 取消上次检测（如果有）
        _allTestCts?.Cancel();
        _allTestCts = new CancellationTokenSource();
        
        AppendLog("开始整机检测...\n");
        _stateService.StartTest("ALL");
        var result = await _detectionService.RunAllTestsAsync(AppendLog,_allTestCts.Token);
        _stateService.ReportTestResult("ALL", result);
    }

    private async Task VoltageTestAsync()
    {
        if (!CanRunTest("Voltage")) return;

        AppendLog("开始AI检测...");

        _stateService.StartTest("Voltage");

        var result = await _detectionService.RunVoltageTestAsync(AppendLog);

        _stateService.ReportTestResult("Voltage", result);
    }
    
    private async Task CommTestAsync()
    {
        DetectLog += "开始通讯检测...\n";
        await _detectionService.RunCommTestAsync(AppendLog);
    }

    private async Task AiTestAsync()
    {
        if (!CanRunTest("Voltage")) return;
        
        AppendLog("开始AI检测...\n");
        
        _stateService.StartTest("AI");
        
        var result = await _detectionService.RunAiTestAsync(AppendLog);
        
        _stateService.ReportTestResult("AI", result);
    }
    
    private async Task DiTestAsync()
    {
        DetectLog += "开始DI检测...\n";
        await _detectionService.RunDiTestAsync(AppendLog);
    }
    
    private async Task DoTestAsync()
    {
        if (!CanRunTest("DO")) return;
        
        DetectLog += "开始DO检测...\n";

        // 取消上次检测（如果有）
        _doTestCts?.Cancel();
        _doTestCts = new CancellationTokenSource();

        try
        {
            _stateService.StartTest("DO");
            
            var success = await _detectionService.RunDoTestAsync(AppendLog, _doTestCts.Token);
            
            _stateService.ReportTestResult("DO", success);
        }
        catch (OperationCanceledException)
        {
            DetectLog += "DO检测已取消\n";
        }
        catch (Exception ex)
        {
            DetectLog += $"DO检测异常: {ex.Message}\n";
        }
    }
    
    private void ExportResults()
    {
        const string filePath = "检测结果.csv";
        _exportService.ExportToCsv(_detectLog, filePath);
        AppendLog($"结果已导出到 {filePath}");
        _stateService.SaveResults();
        
        DetectLog = string.Empty;
    }
    
    private void CancelDoTest()
    {
        if (_doTestCts.IsCancellationRequested) return;
        _doTestCts.Cancel();
        DetectLog += "请求取消DO检测...\n";
    }
    
    private void AppendLog(string message)
    {
        DetectLog += $"{DateTime.Now:HH:mm:ss} - {message}\n";
    }
    
    private bool CanRunTest(string testName)
    {
        if (_stateService.CurrentState == DetectionState.Idle) return true;
        AppendLog($"当前非 Idle 状态，无法开始 {testName} 检测。");
        return false;
    }
}