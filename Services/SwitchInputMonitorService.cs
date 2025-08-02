using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VerifyPro.Enums;
using VerifyPro.Interfaces;
using VerifyPro.Models;
using VerifyPro.Utils;

namespace VerifyPro.Services;

public class SwitchInputMonitorService(
    DeviceCommManager commManager,
    DetectionStateService stateService,
    Func<Task> runAllTests,
    Action cancelDoTest,
    Action<string> log)
{
    private readonly Stopwatch _resetTimer = new();
    private CancellationTokenSource? _cts;

    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        _ = MonitorLoop(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task MonitorLoop(CancellationToken cancellationToken)
    {
        var device = new Device.ModbusTcpDevice
        {
            Name = "开关量输入板卡",
            Ip = "192.168.1.162",
            Port = 502
        };

        IModbusTcpClient? service;
        try
        {
            service = await commManager.GetOrConnectModbusTcpDeviceAsync(device);
            if (service == null)
            {
                return;
            }
        }
        catch (Exception ex)
        {
            log($"连接 Modbus 异常: {ex.Message}");
            return;
        }

        var board = new SwitchInputBoard(service);
        var lastStates = new Dictionary<string, bool?>()
        {
            ["Start"] = null,
            ["Reset"] = null
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await board.RefreshAsync();
            }
            catch (Exception ex)
            {
                log($"板卡刷新异常: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            var start = board.Di12;
            var reset = board.Di13;
            
            // ✅ Start 控制检测启动
            if (start && stateService.CurrentState == DetectionState.Idle)
            {
                log("接收到 Start 信号，开始检测...");
                _ = runAllTests(); // 异步开始
            }

            // ✅ Reset 控制检测复位
            if (reset)
            {
                if (!_resetTimer.IsRunning)
                {
                    _resetTimer.Restart();
                    log("Reset 信号开始计时...");
                }
                else if (_resetTimer.Elapsed.TotalSeconds >= 3)
                {
                    if (stateService.CurrentState == DetectionState.Running)
                    {
                        log("检测进行中，Reset 被触发，取消当前检测！");
                        cancelDoTest();
                    }
                    else
                    {
                        log("Reset 持续 3 秒，程序复位为 Idle");
                        stateService.SaveResults();
                    }

                    _resetTimer.Reset();
                }
            }
            else
            {
                if (_resetTimer.IsRunning)
                {
                    log("Reset 信号中断，计时取消");
                    _resetTimer.Reset();
                }
            }

            await Task.Delay(500, cancellationToken);
        }
    }
}