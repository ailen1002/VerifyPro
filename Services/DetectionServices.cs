using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using VerifyPro.Enums;
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

        var service = await commManager.GetOrConnectTestDeviceAsync(device, log);
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

        var device = new Device.ModbusDevice
        {
            Name = "检测板卡",
            Ip = "192.168.1.150",
            Port = 502
        };

        var service = await commManager.GetOrConnectModbusDeviceAsync(device, log);
        if (service == null)
        {
            log("Modbus 设备连接失败！");
            return;
        }
        
        try
        {
            var resBoard = new ResOutputBoard(service, log);
            
            await resBoard.CloseOddChannels();    // 控制 1, 3, 5, 7... 通道闭合
            await Task.Delay(3000);
            await resBoard.CloseEvenChannels();   // 控制 0, 2, 4, 6... 通道闭合
            await Task.Delay(3000);
            await resBoard.CloseAllChannels();    // 所有通道闭合
            await Task.Delay(3000);
            await resBoard.OpenAllChannels();
        }
        catch (Exception ex)
        {
            log($"写入失败: {ex.Message}");
        }

        log("DI检测完成。");
    }

    public async Task RunDiTestAsync(Action<string> log)
    {
        log("DI检测开始...");

        var device = new Device.ModbusDevice
        {
            Name = "继电器输出板卡",
            Ip = "192.168.1.153",
            Port = 502
        };

        var service = await commManager.GetOrConnectModbusDeviceAsync(device, log);
        if (service == null)
        {
            log("Modbus 设备连接失败！");
            return;
        }

        try
        {
            var outputBoard = new RelayOutputBoard(service, log);
            
            await outputBoard.ApSwitch.On();
            
            await Task.Delay(5000);
            
            await outputBoard.ApSwitch.Off();
        }
        catch (Exception ex)
        {
            log($"写入失败: {ex.Message}");
        }

        log("DI检测完成。");
    }

}
