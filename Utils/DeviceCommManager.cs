using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyPro.Interfaces;
using VerifyPro.Models;
using VerifyPro.Services;

namespace VerifyPro.Utils;

public class DeviceCommManager
{
    private readonly Dictionary<string, IModbusTcpClient> _modbusTcpServices = new();
    private readonly Dictionary<string, IModbusRtuClient> _modbusRtuServices = new();
    private readonly Dictionary<string, ITestDeviceService> _testDeviceServices = new();

    private static string GetKey(string ip, int port) => $"{ip}:{port}";

    // 添加或连接 ModbusTcp 设备
    public async Task<IModbusTcpClient?> GetOrConnectModbusTcpDeviceAsync(Device.ModbusTcpDevice device)
    {
        var key = GetKey(device.Ip, device.Port);

        if (_modbusTcpServices.TryGetValue(key, out var existingService) && existingService.IsConnected)
            return existingService;

        var service = new ModbusTcpService();
        var connected = await service.ConnectAsync(device.Ip, device.Port);

        if (!connected) return null;
        _modbusTcpServices[key] = service;
        
        return service;
    }
    
    // 添加或连接 ModbusTcp 设备
    public async Task<IModbusRtuClient?> GetOrConnectModbusRtuDeviceAsync(Device.ModbusRtuDevice device)
    {
        var key = GetKey(device.SerialPort, device.Baud);

        if (_modbusRtuServices.TryGetValue(key, out var existingService) && existingService.IsConnected)
            return existingService;

        var service = new ModbusRtuService();
        var connected = await service.ConnectAsync(device.SerialPort, device.Baud, device.SlaveId);

        if (!connected) return null;
        _modbusRtuServices[key] = service;
        
        return service;
    }

    // 添加或连接 测试设备（HEX）
    public async Task<ITestDeviceService?> GetOrConnectTestDeviceAsync(Device.TestDevice device)
    {
        var key = GetKey(device.Ip, device.Port);

        if (_testDeviceServices.TryGetValue(key, out var existingService) && existingService.IsConnected)
            return existingService;

        var service = new TestDeviceHexService();
        var connected = await service.ConnectAsync(device.Ip, device.Port);

        if (!connected) return null;
        _testDeviceServices[key] = service;

        return service;
    }

    public async Task DisconnectAllAsync()
    {
        foreach (var svc in _modbusTcpServices.Values)
            await svc.DisconnectAsync();
        
        foreach (var svc in _modbusRtuServices.Values)
            await svc.DisconnectAsync();

        foreach (var svc in _testDeviceServices.Values)
            await svc.DisconnectAsync();

        _modbusTcpServices.Clear();
        _modbusRtuServices.Clear();
        _testDeviceServices.Clear();
    }
}
