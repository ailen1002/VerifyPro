using System;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using VerifyPro.Services;

namespace VerifyPro.ViewModels;

public class MainTestViewModel : ReactiveObject
{   
    // 构造函数注入服务
    private readonly DetectionService _detectionService;
    private readonly ExportService _exportService;
    private ConfigFileViewModel ConfigFileVm { get; }
    public MainTestViewModel(DetectionService detectionService, ExportService exportService, ConfigFileViewModel configFileVm)
    {
        _detectionService = detectionService;
        _exportService = exportService;
        ConfigFileVm = configFileVm;
        _detectLog ="";
        
        StartTestCommand = ReactiveCommand.CreateFromTask(StartTestAsync);
        VoltageTestCommand = ReactiveCommand.CreateFromTask(VoltageTestAsync);
        CommTestCommand = ReactiveCommand.CreateFromTask(CommTestAsync);
        DiTestCommand = ReactiveCommand.CreateFromTask(DiTestAsync);
        ExportResultCommand = ReactiveCommand.Create(ExportResults);

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
    public ReactiveCommand<Unit, Unit> DiTestCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportResultCommand { get; }
    
    private async Task StartTestAsync()
    {
        DetectLog += "开始整机检测...\n";
        await _detectionService.RunAllTestsAsync(AppendLog);
    }

    private async Task VoltageTestAsync()
    {
        DetectLog += "开始电压检测...\n";
        await _detectionService.RunVoltageTestAsync(AppendLog);
    }
    
    private async Task CommTestAsync()
    {
        DetectLog += "开始通讯检测...\n";
        await _detectionService.RunCommTestAsync(AppendLog);
    }

    private async Task DiTestAsync()
    {
        DetectLog += "开始DI检测...\n";
        await _detectionService.RunDiTestAsync(AppendLog);
    }
    private void ExportResults()
    {
        const string filePath = "检测结果.csv";
        _exportService.ExportToCsv(_detectLog, filePath);
        AppendLog($"结果已导出到 {filePath}");
    }
    
    private void AppendLog(string message)
    {
        DetectLog += $"{DateTime.Now:HH:mm:ss} - {message}\n";
    }
}