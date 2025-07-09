using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VerifyPro.Enums;
using VerifyPro.Interfaces;
using VerifyPro.Models;
using VerifyPro.Utils;

namespace VerifyPro.Services;

public class DetectionService(DeviceCommManager commManager)
{
    public async Task RunAllTestsAsync(Action<string> log)
    {
        log("开始所有测试...\n");

        await RunVoltageTestAsync(log);
        await RunCommTestAsync(log);
        await RunAiTestAsync(log);

        log("\n所有测试完成。");
    }

    public async Task RunVoltageTestAsync(Action<string> log)
    {
        log("正在读取电压...");
        await Task.Delay(300);
        log("电压值为 12.1V");
    }

    public async Task RunCommTestAsync(Action<string> log)
    {
        log("通讯检测开始...");

        var device = new Device.TestDevice
        {
            Name = "测试设备",
            Ip = "192.168.1.151",
            Port = 9000
        };

        var service = await commManager.GetOrConnectTestDeviceAsync(device);
        if (service == null)
        {
            log("设备连接失败，检测中止！");
            return;
        }

        var packetA = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
        var packetB = new byte[] { 0x22, 0x33, 0x44, 0x55, 0x66 };

        for (int i = 1; i <= 100; i++)
        {
            var packet = i % 2 == 1 ? packetA : packetB;

            log($"第 {i} 次发送：{BitConverter.ToString(packet)}");
            await service.SendAsync(packet);

            await Task.Delay(1000);
        }

        log("通讯检测完成。");
    }

    public async Task RunAiTestAsync(Action<string> log)
    {
        log("模拟量检测开始...");

        // 准备 Modbus 设备
        var device = new Device.ModbusDevice
        {
            Name = "检测板卡",
            Ip = "192.168.1.150",
            Port = 502
        };

        ICommunicationService? service = null;

        // 连接设备
        try
        {
            service = await commManager.GetOrConnectModbusDeviceAsync(device);

            if (service == null)
            {
                log("Modbus 设备连接失败");
                return;
            }

            log("Modbus 设备连接成功");
        }
        catch (Exception ex)
        {
            log($"连接 Modbus 设备异常: {ex.Message}");
            return;
        }

        // 写入控制命令
        try
        {
            var resBoard = new ResOutputBoard(service);

            log("关闭奇数通道...");
            await resBoard.CloseOddChannels();
            await Task.Delay(3000);

            log("关闭偶数通道...");
            await resBoard.CloseEvenChannels();
            await Task.Delay(3000);

            log("关闭所有通道...");
            await resBoard.CloseAllChannels();
            await Task.Delay(3000);

            log("打开所有通道...");
            await resBoard.OpenAllChannels();

            log("模拟量检测完成。");
        }
        catch (Exception ex)
        {
            log($"写入失败: {ex.Message}");
        }
    }

    public async Task RunDiTestAsync(Action<string> log)
    {
        log("DI 检测开始...");

        var device = new Device.ModbusDevice
        {
            Name = "继电器输出板卡",
            Ip = "192.168.1.153",
            Port = 502
        };

        ICommunicationService? service = null;

        // 连接 Modbus 设备
        try
        {
            service = await commManager.GetOrConnectModbusDeviceAsync(device);

            if (service == null)
            {
                log("Modbus 设备连接失败！");
                return;
            }

            log("Modbus 设备连接成功");
        }
        catch (Exception ex)
        {
            log($"连接 Modbus 设备异常: {ex.Message}");
            return;
        }

        // 控制输出板
        try
        {
            var outputBoard = new RelayOutputBoard(service);

            log("打开 AP 开关...");
            await outputBoard.ApSwitch.On();

            await Task.Delay(5000);

            log("关闭 AP 开关...");
            await outputBoard.ApSwitch.Off();

            log("DI 检测完成。");
        }
        catch (Exception ex)
        {
            log($"控制失败: {ex.Message}");
        }
    }

    public async Task RunDoTestAsync(Action<string> log, CancellationToken cancellationToken)
    {
        log("DO 检测开始...");

        var device = new Device.ModbusDevice
        {
            Name = "开关量输入板卡",
            Ip = "192.168.1.162",
            Port = 502
        };

        ICommunicationService? service;
        try
        {
            service = await commManager.GetOrConnectModbusDeviceAsync(device);
            if (service == null)
            {
                log("Modbus 设备连接失败！");
                return;
            }

            log("Modbus 设备连接成功");
        }
        catch (Exception ex)
        {
            log($"连接 Modbus 设备异常: {ex.Message}");
            return;
        }

        var switchInputBoard = new SwitchInputBoard(service);

        // 通道别名映射（可扩展）
        var inputMappings = new Dictionary<string, Func<bool>>
        {
            ["DI1"] = () => switchInputBoard.Di1,
            ["Start"] = () => switchInputBoard.Di12,
            ["Restart"] = () => switchInputBoard.Di13
        };

        var lastStates = inputMappings.ToDictionary(kv => kv.Key, _ => (bool?)null);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await switchInputBoard.RefreshAsync();

                foreach (var (name, getter) in inputMappings)
                {
                    var current = getter();
                    var previous = lastStates[name];

                    if (previous != null && previous == current) continue;
                    log($"{name} 开关状态: {(current ? "开" : "关")}");
                    lastStates[name] = current;
                }
                
                await Task.Delay(500, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            log("DO 检测被取消。");
        }
        catch (Exception ex)
        {
            log($"轮询检测异常: {ex.Message}");
        }
    }
    
}
