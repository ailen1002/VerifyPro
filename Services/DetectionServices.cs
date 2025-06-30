using System;
using System.Threading.Tasks;

namespace VerifyPro.Services;

public class DetectionService()
{
    public async Task RunAllTestsAsync(Action<string> log)
    {
        await RunVoltageTestAsync(log);
        await RunCommTestAsync(log);
        await RunAiTestAsync(log);
        // 其他检测项...
    }

    public async Task RunVoltageTestAsync(Action<string> log)
    {
        log("正在读取电压...");
        await Task.Delay(300);
        log("电压值为 12.1V");
    }
    
    private async Task RunCommTestAsync(Action<string> log)
    {
        log("通讯检测开始...");
        await Task.Delay(300);
        log($"通讯检测结果: 正常");
    }
    
    private async Task RunAiTestAsync(Action<string> log)
    {
        log("模拟量检测开始...");
        await Task.Delay(300);
        log($"模拟量检测结果: 正常");
    }
}