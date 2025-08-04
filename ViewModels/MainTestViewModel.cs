using System;
using System.IO;
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
        
        // 初始化
        InitializeProductKey();
        
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
    
    // 序列号
    private string _productKey = "";
    public string ProductKey
    {
        get => _productKey;
        set => this.RaiseAndSetIfChanged(ref _productKey, value);
    }
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
        if (!CanRunTest("Comm")) return;
        
        AppendLog("开始通讯检测...");
        
        _stateService.StartTest("Comm");
        
        var result = await _detectionService.RunCommTestAsync(AppendLog);
        
        _stateService.ReportTestResult("Comm", result);
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
        if (!CanRunTest("DI")) return;
        
        AppendLog("开始DI检测...\n");
        
        _stateService.StartTest("DI");
        
        var result = await _detectionService.RunDiTestAsync(AppendLog);
        
        _stateService.ReportTestResult("DI", result);
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
    
    private void InitializeProductKey()
    {
        if (string.IsNullOrWhiteSpace(ModelName))
            throw new InvalidOperationException("ModelName 不能为空");

        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var modelFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModelName);
        var dateFolderPath = Path.Combine(modelFolderPath, datePart);

        var serialNumber = 1;
        if (Directory.Exists(dateFolderPath))
        {
            var existingFiles = Directory.GetFiles(dateFolderPath, "*.csv");
            serialNumber = existingFiles.Length + 1;
        }

        ProductKey = $"{datePart}{serialNumber:D8}";
    }
    
    private void ExportResults()
    {
        if (string.IsNullOrWhiteSpace(ModelName)) return;

        var datePart = DateTime.Now.ToString("yyyyMMdd");
        var modelFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ModelName);
        var dateFolderPath = Path.Combine(modelFolderPath, datePart);

        if (!Directory.Exists(dateFolderPath))
        {
            Directory.CreateDirectory(dateFolderPath);
        }

        var filePath = Path.Combine(dateFolderPath, $"{ProductKey}.csv");

        var header = CreateCenteredLine(ProductKey);
        var footer = CreateCenteredLine(CurrentState.ToString() ?? "Unknown");
        
        var content = $"{header}\n{_detectLog}\n{footer}";

        _exportService.ExportToCsv(content, filePath);

        // 序列号自增
        if (ProductKey.Length == 16)
        {
            var prefix = ProductKey[..8]; // 日期
            var serialStr = ProductKey[8..]; // 序号
            if (int.TryParse(serialStr, out var serialNumber))
            {
                serialNumber += 1;
                ProductKey = prefix + serialNumber.ToString("D8");
            }
        }

        _stateService.SaveResults();
        DetectLog = string.Empty;
    }
    private static string CreateCenteredLine(string content, int totalWidth = 60, char fillChar = '*')
    {
        if (string.IsNullOrEmpty(content))
            return new string(fillChar, totalWidth);

        content = $"【{content}】";
        var contentLength = content.Length;

        if (contentLength >= totalWidth)
            return content;

        var padding = totalWidth - contentLength;
        var left = padding / 2;
        var right = padding - left;

        return new string(fillChar, left) + content + new string(fillChar, right);
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