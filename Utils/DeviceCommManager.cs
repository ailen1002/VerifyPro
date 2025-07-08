using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VerifyPro.Interfaces;
using VerifyPro.Models;
using VerifyPro.Services;

namespace VerifyPro.Utils;

public class DeviceCommManager
{
    private readonly Dictionary<string, ICommunicationService> _modbusServices = new();
    private readonly Dictionary<string, ICommunicationService> _testDeviceServices = new();

    private string GetKey(string ip, int port) => $"{ip}:{port}";

    // 添加或连接 Modbus 设备
    public async Task<ICommunicationService?> GetOrConnectModbusDeviceAsync(Device.ModbusDevice device)
    {
        var key = GetKey(device.Ip, device.Port);

        if (_modbusServices.TryGetValue(key, out var existingService) && existingService.IsConnected)
            return existingService;

        var service = new ModbusTcpService();
        var connected = await service.ConnectAsync(device.Ip, device.Port);

        if (!connected) return null;
        _modbusServices[key] = service;
        
        return service;
    }

    // 添加或连接 测试设备（HEX）
    public async Task<ICommunicationService?> GetOrConnectTestDeviceAsync(Device.TestDevice device)
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
        foreach (var svc in _modbusServices.Values)
            await svc.DisconnectAsync();

        foreach (var svc in _testDeviceServices.Values)
            await svc.DisconnectAsync();

        _modbusServices.Clear();
        _testDeviceServices.Clear();
    }
}
