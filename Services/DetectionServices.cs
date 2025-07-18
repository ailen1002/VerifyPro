﻿using System;
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
using VerifyPro.ViewModels;

namespace VerifyPro.Services;

public class DetectionService(DeviceCommManager commManager, ConfigFileViewModel configFileViewModel)
{
    private readonly Config _config = configFileViewModel.Config ?? throw new InvalidOperationException();
    public async Task<bool> RunAllTestsAsync(Action<string> log, CancellationToken cancellationToken)
    {
        log("开始所有测试...\n");

        try
        {
            var voltagePassed = await RunVoltageTestAsync(log);
            if (!voltagePassed) return false;

            var doPassed = await RunDoTestAsync(log, cancellationToken);
            if (!doPassed) return false;
        }
        catch (Exception ex)
        {
            log($"测试中发生异常: {ex.Message}");
            return false;
        }

        log("\n所有测试完成。");
        
        return true;
    }

    public async Task<bool> RunVoltageTestAsync(Action<string> log)
    {
        log("模拟量检测开始...");

        var device = new Device.ModbusDevice
        {
            Name = "电压检测板卡",
            Ip = "192.168.1.155",
            Port = 502
        };

        try
        {
            var service = await commManager.GetOrConnectModbusDeviceAsync(device);
            if (service == null)
            {
                log("Modbus 电压板卡连接失败");
                return false;
            }

            log("Modbus 电压板卡连接成功");

            var board = new VoltageInputBoard(service);
            await board.ReadVoltageAsync();

            // 电压检测点定义（名称, 读取值, 最小值, 最大值）
            var voltageChecks = new (string Name, float Value, float Min, float Max)[]
            {
                ("HIC-power", board.Volt1, 21.49f, 23.4f),
                //("crPower_18V", board.Volt2, 20.32f, 21.19f),
                //("crPower_16V", board.Volt3, 16.06f, 16.68f),
                //("crPower_15V", board.Volt4, 14.6f, 15.4f),
                //("crPower_14V", board.Volt5, 14.43f, 15.73f),
                //("crPower_12V", board.Volt6, 11.38f, 12.63f),
                //("crPower_5V", board.Volt7, 4.75f, 5.25f),
                //("hicPower_15V", board.Volt8, 14.99f, 16.01f),
                //("hicPower_15FMV", board.Volt9, 15.4f, 16.78f),
                //("hicPower_12V", board.Volt10, 11.38f, 12.63f),
                //("hicPower_5V", board.Volt11, 4.75f, 5.25f),
                //("fanPower_5V", board.Volt12, 4.75f, 5.25f)
            };

            var allPass = true;

            foreach (var (name, value, min, max) in voltageChecks)
            {
                log($"{name} 电压范围 {min}-{max}V");
                log($"{name} 实测电压 {value}V");

                if (value < min || value > max)
                {
                    log($"⚠️ {name} 电压超出范围！");
                    allPass = false;
                }

                await Task.Delay(100); // 控制日志节奏
            }

            log("模拟量检测完成。");
            return allPass;
        }
        catch (Exception ex)
        {
            log($"电压检测失败: {ex.Message}");
            return false;
        }
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

    public async Task<bool> RunAiTestAsync(Action<string> log)
    {
        log("模拟量检测开始...");

        // 准备 Modbus 设备
        var device1 = new Device.ModbusDevice
        {
            Name = "检测板卡",
            Ip = "192.168.1.150",
            Port = 502
        };

        var device2 = new Device.TestDevice
        {
            Name = "测试设备",
            Ip = "192.168.1.156",
            Port = 9000
        };

        ICommunicationService? service1 = null;
        ICommunicationService? service2 = null;
        // 连接设备
        try
        {
            service1 = await commManager.GetOrConnectModbusDeviceAsync(device1);
            service2 = await commManager.GetOrConnectTestDeviceAsync(device2);
            
            if (service1 == null)
            {
                log("Modbus 设备连接失败");
                return false;
            }
            
            if (service2 == null)
            {
                log("Test 设备连接失败");
                return false;
            }

            log("Modbus Test 设备连接成功");
        }
        catch (Exception ex)
        {
            log($"连接 Modbus 设备异常: {ex.Message}");
            return false;
        }

        // 写入控制命令
        try
        {
            var lowTempSensorLowL = _config.LOW_TEMP_SENSOR_LOW_L;
            log("下限：" + lowTempSensorLowL);
            var resBoard = new ResOutputBoard(service1);
            var command = new byte[] { 0xE5, 0xFE, 0x11, 0x03, 0x00, 0x61, 0x00 };
            var command1 = new byte[] { 0xE5, 0xFE, 0x15, 0x04, 0x00, 0x21, 0x02, 0x00};
            var fullCommand = BuildCommandWithChecksum(command);
            var fullCommand1 = BuildCommandWithChecksum(command1);
            log("关闭奇数通道...");
            await resBoard.CloseOddChannels();
            await Task.Delay(1000);
            var res = await service2.SendAsync(fullCommand);
            log("接收数据: " + BitConverter.ToString(res));
            await Task.Delay(10000);
            var res1 = await service2.SendAsync(fullCommand1);
            log("接收数据: " + BitConverter.ToString(res1));
            var labels = new[]
            {
                "HPS", "LPS", "DISCH1", "OIL1", "TO",
                "EXL1", "EXL2", "EXG1", "EXG2", "SCT", "SCG1", "SCL1"
            };

            const int startIndex = 15;
    
            for (var i = 0; i < labels.Length; i++)
            {
                int high = res1[startIndex + i * 2];
                int low = res1[startIndex + i * 2 + 1];
                var rawValue = (high << 8) | low;

                var scaledValue = i is 0 or 1
                    ? rawValue / 1000.0
                    : rawValue / 10.0;

                log($"{labels[i]}: {scaledValue:F3}");
            }
            
            log("关闭偶数通道...");
            await resBoard.CloseEvenChannels();
            await Task.Delay(10000);
            res1 = await service2.SendAsync(fullCommand1);
            for (var i = 0; i < labels.Length; i++)
            {
                int high = res1[startIndex + i * 2];
                int low = res1[startIndex + i * 2 + 1];
                var rawValue = (high << 8) | low;

                var scaledValue = i is 0 or 1
                    ? rawValue / 1000.0
                    : rawValue / 10.0;

                log($"{labels[i]}: {scaledValue:F3}");
            }
            
            log("打开所有通道...");
            await resBoard.OpenAllChannels();
            await Task.Delay(10000);
            res1 = await service2.SendAsync(fullCommand1);
            for (var i = 0; i < labels.Length; i++)
            {
                int high = res1[startIndex + i * 2];
                int low = res1[startIndex + i * 2 + 1];
                var rawValue = (high << 8) | low;

                var scaledValue = i is 0 or 1
                    ? rawValue / 1000.0
                    : rawValue / 10.0;

                log($"{labels[i]}: {scaledValue:F3}");
            }
            log("模拟量检测完成。");
        }
        catch (Exception ex)
        {
            log($"写入失败: {ex.Message}");
        }

        return true;
    }
    
    private static byte[] BuildCommandWithChecksum(byte[] command)
    {
        var checksum = command.Aggregate<byte, byte>(0x00, (current, b) => (byte)(current ^ b));
        
        var fullCommand = new byte[command.Length + 1];
        Array.Copy(command, fullCommand, command.Length);
        fullCommand[^1] = checksum;

        return fullCommand;
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
