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
    public async Task<ICommunicationService?> GetOrConnectModbusDeviceAsync(Device.ModbusDevice device, Action<string>? log = null)
    {
        var key = GetKey(device.Ip, device.Port);

        if (_modbusServices.TryGetValue(key, out var existingService) && existingService.IsConnected)
            return existingService;

        var service = new ModbusTcpService();
        var connected = await service.ConnectAsync(device.Ip, device.Port);

        if (connected)
        {
            _modbusServices[key] = service;
            log?.Invoke($"已连接 Modbus 设备 {key}");
            return service;
        }

        log?.Invoke($"连接 Modbus 设备 {key} 失败");
        return null;
    }

    // 添加或连接 测试设备（HEX）
    public async Task<ICommunicationService?> GetOrConnectTestDeviceAsync(Device.TestDevice device, Action<string>? log = null)
    {
        var key = GetKey(device.Ip, device.Port);

        if (_testDeviceServices.TryGetValue(key, out var existingService) && existingService.IsConnected)
            return existingService;

        var service = new TestDeviceHexService();
        var connected = await service.ConnectAsync(device.Ip, device.Port);

        if (connected)
        {
            _testDeviceServices[key] = service;
            log?.Invoke($"已连接测试设备 {key}");
            return service;
        }

        log?.Invoke($"连接测试设备 {key} 失败");
        return null;
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
