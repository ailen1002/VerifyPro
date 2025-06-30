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
    public MainTestViewModel(DetectionService detectionService, ExportService exportService)
    {
        _detectionService = detectionService;
        _exportService = exportService;
        _detectLog ="";
        
        StartTestCommand = ReactiveCommand.CreateFromTask(StartTestAsync);
        VoltageTestCommand = ReactiveCommand.CreateFromTask(VoltageTestAsync);
        ExportResultCommand = ReactiveCommand.Create(ExportResults);
    }
    
    // 属性绑定到界面用于显示检测日志
    private string _detectLog;
    public string DetectLog
    {
        get => _detectLog;
        set => this.RaiseAndSetIfChanged(ref _detectLog, value);
    }
    
    public ReactiveCommand<Unit, Unit> StartTestCommand { get; }
    public ReactiveCommand<Unit, Unit> VoltageTestCommand { get; }
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