using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Models;
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

        for (var i = 1; i <= 100; i++)
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

    public async Task RunSwitchInputAsync(Action<string> log, CancellationToken cancellationToken)
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

    public async Task<bool> RunDoTestAsync(Action<string> log, CancellationToken cancellationToken)
    {
        log("DO 检测开始...");

        // 初始化设备
        var (service1, service2, service3) = await InitializeDevicesAsync(log);
        if (service1 is null || service2 is null || service3 is null)
        {
            log("初始化设备失败");
            return false;
        }

        var acInputBoard = new InputBoard(service1);
        var dcInputBoard = new InputBoard(service2);
        var outputBoard = new RelayOutputBoard(service3);

        var inputMappings = GetInputMappings(acInputBoard, dcInputBoard);
        var lastStates = inputMappings.ToDictionary(kv => kv.Key, _ => (bool?)null);
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await outputBoard.TestSwitch.On();
            stopwatch.Restart();

            while (!cancellationToken.IsCancellationRequested && stopwatch.Elapsed.TotalSeconds <= 20)
            {
                await RefreshBoardsAsync(acInputBoard, dcInputBoard);
                
                var error = inputMappings.Any(kv => !kv.Value());

                if (!error)
                {
                    foreach (var (name, getter) in inputMappings)
                    {
                        var current = getter();
                        var previous = lastStates[name];

                        if (previous == current) continue;
                        log($"{name} 输出状态: {(current ? "闭合" : "断开")}");
                        lastStates[name] = current;
                    }
                    return true;
                }
                
                await Task.Delay(500, cancellationToken);
            }
            
            foreach (var (name, getter) in inputMappings)
            {
                var current = getter();
                var previous = lastStates[name];

                if (previous == current) continue;
                log($"{name} 输出状态: {(current ? "闭合" : "断开")}");
                lastStates[name] = current;
            }
            
            var closedChannels = inputMappings
                .Where(kv => !kv.Value())
                .Select(kv => kv.Key)
                .ToList();
            
            if (closedChannels.Count != 0)
            {
                await ShowDetectionWarningAsync("继电器动作检测失败");
                return false;
            }
            else
            {
                return true;
            }
        }
        catch (OperationCanceledException)
        {
            log("DO 检测被取消。");
            return false;
        }
        catch (Exception ex)
        {
            log($"轮询检测异常: {ex.Message}");
            return false;
        }
        finally
        {
            try
            {
                await acInputBoard.ReSetAsync();
                await dcInputBoard.ReSetAsync();
                await outputBoard.TestSwitch.Off();
            }
            catch (Exception ex)
            {
                log($"轮询检测异常: {ex.Message}");
            }

            log("DO 检测完成。");
        }
    }

    // 初始化设备方法
    private async Task<(ICommunicationService?, ICommunicationService?,ICommunicationService?)> InitializeDevicesAsync(Action<string> log)
    {
        var device1 = new Device.ModbusDevice { Name = "AC输入板卡", Ip = "192.168.1.160", Port = 502 };
        var device2 = new Device.ModbusDevice { Name = "DC输入板卡", Ip = "192.168.1.157", Port = 502 };
        var device3 = new Device.ModbusDevice { Name = "继电器输出板卡", Ip = "192.168.1.153", Port = 502 };

        try
        {
            var s1 = await commManager.GetOrConnectModbusDeviceAsync(device1);
            if (s1 == null)
            {
                log("AC输入板卡连接失败！");
                return (null, null,null);
            }

            var s2 = await commManager.GetOrConnectModbusDeviceAsync(device2);
            if (s2 == null)
            {
                log("DC输入板卡连接失败！");
                return (null, null,null);
            }
            var s3 = await commManager.GetOrConnectModbusDeviceAsync(device3);
            if (s3 == null)
            {
                log("DC输入板卡连接失败！");
                return (null, null,null);
            }

            log("Modbus 设备连接成功");
            return (s1, s2, s3);
        }
        catch (Exception ex)
        {
            log($"连接 Modbus 设备异常: {ex.Message}");
            return (null, null,null);
        }
    }
    // 获取通道映射方法
    private static Dictionary<string, Func<bool>> GetInputMappings(InputBoard ac, InputBoard dc)
    {
        return new Dictionary<string, Func<bool>>
        {
            ["20S"] = () => ac.Di1,
            ["O2"] = () => ac.Di2,
            ["VALVE"] = () => ac.Di3,
            //["PDV"] = () => ac.Di4,
            ["OVER"] = () => ac.Di5,
            ["LPBV"] = () => ac.Di6,
            ["SAVE"] = () => ac.Di7,
            ["CH2"] = () => ac.Di8,
            ["CH1"] = () => ac.Di9,
            ["MOV1-1"] = () => dc.Di1,
            ["MOV1-2"] = () => dc.Di2,
            ["MOV1-3"] = () => dc.Di3,
            ["MOV1-4"] = () => dc.Di4,
            //["MOV2-1"] = () => dc.Di5,
            //["MOV2-2"] = () => dc.Di6,
            //["MOV2-3"] = () => dc.Di7,
            //["MOV2-4"] = () => dc.Di8,
            //["MOV3-1"] = () => dc.Di9,
            //["MOV3-2"] = () => dc.Di10,
            //["MOV3-3"] = () => dc.Di11,
            //["MOV3-4"] = () => dc.Di12,
        };
    }
    // 刷新通道方法
    private static async Task RefreshBoardsAsync(InputBoard ac, InputBoard dc)
    {
        await ac.RefreshAsync();
        await dc.RefreshAsync();
    }
    // 弹窗提示
    private static Task ShowDetectionWarningAsync(string message)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ButtonDefinitions = new List<ButtonDefinition>
            {
                new() { Name = "Yes", },
            },
            ContentTitle = "title",
            ContentMessage = message,
            Icon = MsBox.Avalonia.Enums.Icon.Error,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Width = 250,
            Height = 150,
        }).ShowAsync();
        return Task.CompletedTask;
    }
    
}
