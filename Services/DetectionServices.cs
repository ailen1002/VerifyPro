using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

            var commPassed = await RunCommTestAsync(log);
            if (!commPassed) return false;
            
            var aiPassed = await RunAiTestAsync(log);
            if (!aiPassed) return false;
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
    
    public async Task<bool> RunCommTestAsync(Action<string> log)
    {
        // 准备设备
        var device1 = new Device.ModbusDevice { Name = "检测板卡", Ip = "192.168.1.151", Port = 502 };
        var device2 = new Device.TestDevice { Name = "测试设备", Ip = "192.168.1.156", Port = 9000 };

        // 连接设备
        IModbusClient? service1 = null;
        ITestDeviceService? service2 = null;
        
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
            log($"连接设备异常: {ex.Message}");
            return false;
        }

        try
        {
            if (!await ExecuteCommandAsync(service2, _config.SYSTEM_STOP_TxData, 15, "系统停止命令", log))
                return false;

            await Task.Delay(1000);

            if (!await ExecuteCommandAsync(service2, _config.EEPROM_INITIAL_TxData, 14, "EEPROM初始化", log))
                return false;

            await Task.Delay(1000);
            
            if (!await ExecuteCommandAsync(service2, _config.NOURYOKU_16HP, 16, "能力设置", log))
                return false;
            
            await Task.Delay(1000);
            
            if (!await ExecuteCommandAsync(service2, _config.SYSTEM_REBOOT_TxData, 14, "系统重启", log))
                return false;

            await Task.Delay(1000);
            
            var ready = await WaitForDeviceReadyAsync(
                service2,
                _config.SYSTEM_STOP_TxData,
                15,
                timeoutMs: 10000,
                pollingIntervalMs: 1000,
                commandName: "状态查询",
                log);

            if (!ready)
            {
                log?.Invoke("设备未准备好，超时退出");
                return false;
            }
            
            if (!await ExecuteCommandAsync(service2, _config.SYSTEM_STOP_TxData, 15, "系统停止命令", log))
                return false;

            await Task.Delay(1000);
            
            var a = _config.PARAMETER_CHECK_TxData;
            var parts = a.Split(", ");
            parts[^1] = "0x80";
            var txData = string.Join(", ", parts);
            var command = BuildCommandWithChecksum(txData);
            log($"Tx: {BitConverter.ToString(command).Replace("-", " ")}");
            var expectedLength = 19;
            var commandName = "冷媒种类查询";
            var result = await service2.SetTxCommand(command, expectedLength, commandName);
            log($"Rx: {BitConverter.ToString(result.Response).Replace("-", " ")}");
            
            if (result.Success)
            {
                if (result.Response[15] == Convert.ToByte(_config.READ_REIBAI))
                {
                    log($"接收：{result.ActualLength}字节，预期：{expectedLength}字节。");
                    log($"{result.CommandName}发送成功");
                    log($"冷媒种类读出值: 0x{result.Response[15]:x2},预期: 0x{Convert.ToByte(_config.READ_REIBAI):x2}");
                }
                else
                {
                    log($"接收：{result.ActualLength}字节，预期：{expectedLength}字节。");
                    log($"{result.CommandName}发送成功");
                    log($"冷媒种类读出值: 0x{result.Response[15]:x2},预期{Convert.ToByte(_config.READ_REIBAI):x2}");
                    return false;
                }
            }
            else
            {
                log($"接收：{result.ActualLength}字节，预期：{expectedLength}字节。");
                log($"{result.CommandName}发送失败");
                return false;
            }

            await Task.Delay(1000);
            
            parts[^1] = "0x81";
            txData = string.Join(", ", parts);
            command = BuildCommandWithChecksum(txData);
            log($"Tx: {BitConverter.ToString(command).Replace("-", " ")}");
            commandName = "能力设置查询";
            var result1 = await service2.SetTxCommand(command, expectedLength, commandName);
            log($"Rx: {BitConverter.ToString(result1.Response).Replace("-", " ")}");
            
            if (result1.Success)
            {
                if (result1.Response[15] == Convert.ToByte(_config.READ_NOURYOKU_16HP))
                {
                    log($"接收：{result1.ActualLength}字节，预期：{expectedLength}字节。");
                    log($"{result1.CommandName}发送成功");
                    log($"能力设置读出值: 0x{result1.Response[15]:x2},预期: 0x{Convert.ToByte(_config.READ_NOURYOKU_16HP):x2}");
                }
                else
                {
                    log($"接收：{result1.ActualLength}字节，预期：{expectedLength}字节。");
                    log($"{result1.CommandName}发送成功");
                    log($"能力设置读出值: 0x{result1.Response[15]:x2},预期: 0x{Convert.ToByte(_config.READ_NOURYOKU_16HP):x2}");
                    return false;
                }
            }
            else
            {
                log($"接收：{result1.ActualLength}字节，预期：{expectedLength}字节。");
                log($"{result1.CommandName}发送失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            log($"检测异常: {ex.Message}");
            return false;
        }

        return true;
    }

    public async Task<bool> RunAiTestAsync(Action<string> log)
    {
        log("模拟量检测开始...");

        // 准备设备
        var device1 = new Device.ModbusDevice { Name = "检测板卡", Ip = "192.168.1.150", Port = 502 };
        var device2 = new Device.TestDevice { Name = "测试设备", Ip = "192.168.1.156", Port = 9000 };

        // 连接设备
        IModbusClient? service1 = null;
        ITestDeviceService? service2 = null;

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
            log($"连接设备异常: {ex.Message}");
            return false;
        }

        try
        {
            // 加载配置值
            var config = LoadDetectionThresholds();

            var resBoard = new ResOutputBoard(service1);
            var stopCommand = BuildCommandWithChecksum(_config.SYSTEM_STOP_TxData);
            var detectCommand = BuildCommandWithChecksum(_config.AD_CHECK_TxData);

            // 初始化日志与检测状态
            var hasOutOfRange = false;
            StringBuilder outOfRangeLog = new();

            // 低点检测：全部关闭
            await resBoard.CloseAllChannels();
            await service2.SystemStop(stopCommand);
            await Task.Delay(10000);
            var res1 = await RetryAnalogDetectionAsync(service2, detectCommand, log: log);
            if (res1 == null)
            {
                await ShowDetectionWarningAsync("模拟量检测失败，无法获取有效数据");
                return false;
            }
            log("低点检测数据: " + BitConverter.ToString(res1));
            EvaluateData(res1, log, config, ref hasOutOfRange, outOfRangeLog, DetectionMode.Low);

            // 奇数高点 偶数低点
            await resBoard.CloseEvenChannels();
            await Task.Delay(10000);
            res1 = await RetryAnalogDetectionAsync(service2, detectCommand, log: log);
            if (res1 == null)
            {
                await ShowDetectionWarningAsync("模拟量检测失败，无法获取有效数据");
                return false;
            }
            log("交替检测数据: " + BitConverter.ToString(res1));
            EvaluateData(res1, log, config, ref hasOutOfRange, outOfRangeLog, DetectionMode.Mixed);

            // 全部高点检测
            await resBoard.OpenAllChannels();
            await Task.Delay(10000);
            res1 = await RetryAnalogDetectionAsync(service2, detectCommand, log: log);
            if (res1 == null)
            {
                await ShowDetectionWarningAsync("模拟量检测失败，无法获取有效数据");
                return false;
            }
            log("高点检测数据: " + BitConverter.ToString(res1));
            EvaluateData(res1, log, config, ref hasOutOfRange, outOfRangeLog, DetectionMode.High);

            // 弹窗提示并中断
            if (hasOutOfRange)
            {
                await ShowDetectionWarningAsync(outOfRangeLog.ToString());
                return false;
            }
        }
        catch (Exception ex)
        {
            log($"检测异常: {ex.Message}");
            return false;
        }

        return true;
    }
    
    private static async Task<byte[]?> RetryAnalogDetectionAsync(
        ITestDeviceService service,
        byte[] command,
        int maxRetries = 10,
        int delayMs = 1000,
        Action<string>? log = null)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await service.AnalogDetection(command);
                if (result.Length > 20)
                    return result;

                log?.Invoke($"第 {attempt} 次读取失败，响应为空或数据异常");
            }
            catch (Exception ex)
            {
                log?.Invoke($"第 {attempt} 次读取异常: {ex.Message}");
            }

            if (attempt < maxRetries)
                await Task.Delay(delayMs);
        }

        return null;
    }

    private enum DetectionMode
    {
        Low,
        Mixed,
        High
    }

    private class Thresholds
    {
        public double LowTempLowL, LowTempLowH, LowTempHighL, LowTempHighH;
        public double HighTempLowL, HighTempLowH, HighTempHighL, HighTempHighH;
        public double PressureLowL, PressureLowH, PressureHighL, PressureHighH;
    }

    private Thresholds LoadDetectionThresholds()
    {
        return new Thresholds
        {
            LowTempLowL = double.Parse(_config.LOW_TEMP_SENSOR_LOW_L),
            LowTempLowH = double.Parse(_config.LOW_TEMP_SENSOR_LOW_H),
            LowTempHighL = double.Parse(_config.LOW_TEMP_SENSOR_HIGH_L),
            LowTempHighH = double.Parse(_config.LOW_TEMP_SENSOR_HIGH_H),
            HighTempLowL = double.Parse(_config.HIGH_TEMP_SENSOR_LOW_L),
            HighTempLowH = double.Parse(_config.HIGH_TEMP_SENSOR_LOW_H),
            HighTempHighL = double.Parse(_config.HIGH_TEMP_SENSOR_HIGH_L),
            HighTempHighH = double.Parse(_config.HIGH_TEMP_SENSOR_HIGH_H),
            PressureLowL = double.Parse(_config.PRESSURE_SENSOR_LOW_L),
            PressureLowH = double.Parse(_config.PRESSURE_SENSOR_LOW_H),
            PressureHighL = double.Parse(_config.PRESSURE_SENSOR_HIGH_L),
            PressureHighH = double.Parse(_config.PRESSURE_SENSOR_HIGH_H)
        };
    }
    
    private static void EvaluateData(
        IReadOnlyList<byte> res,
        Action<string> log,
        Thresholds config,
        ref bool hasOutOfRange,
        StringBuilder outOfRangeLog,
        DetectionMode mode)
    {
        string[] labels = ["HPS", "LPS", "DISCH1", "OIL1", "TO", "EXL1", "EXL2", "EXG1", "EXG2", "SCT", "SCG1", "SCL1"];
        const int startIndex = 15;

        for (var i = 0; i < labels.Length; i++)
        {
            int high = res[startIndex + i * 2];
            int low = res[startIndex + i * 2 + 1];
            var raw = (high << 8) | low;
            var value = i is 0 or 1 ? raw / 1000.0 : raw / 10.0;

            double min, max;

            switch (mode)
            {
                case DetectionMode.Low:
                    (min, max) = i switch
                    {
                        0 or 1 => (config.PressureLowL, config.PressureLowH),
                        2 => (config.HighTempLowL, config.HighTempLowH),
                        _ => (config.LowTempLowL, config.LowTempLowH)
                    };
                    break;

                case DetectionMode.High:
                    (min, max) = i switch
                    {
                        0 or 1 => (config.PressureHighL, config.PressureHighH),
                        2 => (config.HighTempHighL, config.HighTempHighH),
                        _ => (config.LowTempHighL, config.LowTempHighH)
                    };
                    break;

                case DetectionMode.Mixed:
                    var isHigh = i % 2 == 1;
                    (min, max) = i switch
                    {
                        0 or 1 => isHigh ? (config.PressureHighL, config.PressureHighH) : (config.PressureLowL, config.PressureLowH),
                        2 => isHigh ? (config.HighTempHighL, config.HighTempHighH) : (config.HighTempLowL, config.HighTempLowH),
                        _ => isHigh ? (config.LowTempHighL, config.LowTempHighH) : (config.LowTempLowL, config.LowTempLowH)
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            var inRange = value >= min && value <= max;
            log($"{labels[i]}: {value:F3} | Range: [{min}, {max}] | In Range: {inRange}");

            if (inRange) continue;
            hasOutOfRange = true;
            outOfRangeLog.AppendLine($"{labels[i]} 超出范围: {value:F3}，应在 [{min}, {max}]");
        }
    }
    
    private static byte[] BuildCommandWithChecksum(string hexString)
    {
        // 1. 去除空格，并按逗号分割
        var byteStrings = hexString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase));

        // 2. 转换为 byte 数组
        var command = byteStrings
            .Select(s => Convert.ToByte(s, 16))
            .ToArray();

        // 3. 计算异或校验
        var checksum = command.Aggregate<byte, byte>(0x00, (current, b) => (byte)(current ^ b));

        // 4. 生成完整命令
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

        IModbusClient? service;

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

        IModbusClient? service;
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
    private async Task<(IModbusClient? s1, IModbusClient? s2, IModbusClient? s3)> InitializeDevicesAsync(Action<string> log)
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

    private static async Task<bool> ExecuteCommandAsync(ITestDeviceService service, string txData, int expectedLength,
        string commandName, Action<string> log)
    {
        var command = BuildCommandWithChecksum(txData);
        log($"Tx: {BitConverter.ToString(command).Replace("-", " ")}");

        var result = await service.SetTxCommand(command, expectedLength, commandName);
        log($"Rx: {BitConverter.ToString(result.Response).Replace("-", " ")}");

        if (result.Success)
        {
            log($"接收：{result.ActualLength}字节，预期：{expectedLength}字节。");
            log($"{result.CommandName}发送成功");
            return true;
        }
        else
        {
            log($"接收：{result.ActualLength}字节，预期：{expectedLength}字节。");
            log($"{result.CommandName}发送失败");
            return false;
        }
    }

    private static async Task<bool> WaitForDeviceReadyAsync(
        ITestDeviceService service,
        string txData,
        int expectedLength,
        int timeoutMs = 10000,
        int pollingIntervalMs = 1000,
        string commandName = "系统停止",
        Action<string>? log = null)
    {
        var start = Environment.TickCount;

        while (Environment.TickCount - start < timeoutMs)
        {
            var command = BuildCommandWithChecksum(txData);
            var result = await service.SetTxCommand(command, expectedLength, commandName);

            if (result.Success)
            {
                return true;
            }
            await Task.Delay(pollingIntervalMs);
        }

        return false;
    }
    // private static async Task<bool> ParameterCheckAsync(ITestDeviceService service, string txData, int expectedLength,
    //     string commandName, Action<string> log)
    // {
    //
    // }
}
