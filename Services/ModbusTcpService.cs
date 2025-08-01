using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Modbus.Device;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class ModbusTcpService : IModbusTcpClient
{
    private TcpClient? _client;
    private IModbusMaster? _master;

    public bool IsConnected => _client?.Connected ?? false;

    public async Task<bool> ConnectAsync(string ip, int port)
    {
        _client = new TcpClient();
        await _client.ConnectAsync(ip, port);
        _master = ModbusIpMaster.CreateIp(_client);
        return true;
    }

    public async Task DisconnectAsync()
    {
        _client?.Close();
        _client?.Dispose();
        _client = null;
        _master = null;
        await Task.CompletedTask;
    }
    
    public async Task<byte[]> SendAsync(byte[] data)
    {
        if (_master == null)
            throw new InvalidOperationException("Modbus 主站未连接");

        var functionCode = data[0];

        return functionCode switch
        {
            0x03 => await HandleReadHoldingRegistersAsync(data),
            0x04 => await HandleReadInputRegistersAsync(data),
            0x06 => await HandleWriteSingleRegisterAsync(data),
            0x10 => await HandleWriteMultipleRegistersAsync(data),
            _ => throw new NotSupportedException($"功能码 {functionCode:X2} 未实现")
        };
    }

    private async Task<byte[]> HandleReadHoldingRegistersAsync(IReadOnlyList<byte> data)
    {
        var start = (ushort)(data[1] << 8 | data[2]);
        var count = (ushort)(data[3] << 8 | data[4]);

        var values = await Task.Run(() => _master!.ReadHoldingRegisters(1, start, count));
        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }

    private async Task<byte[]> HandleReadInputRegistersAsync(IReadOnlyList<byte> data)
    {
        var start = (ushort)(data[1] << 8 | data[2]);
        var count = (ushort)(data[3] << 8 | data[4]);

        var values = await Task.Run(() => _master!.ReadInputRegisters(1, start, count));
        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }

    private async Task<byte[]> HandleWriteSingleRegisterAsync(IReadOnlyList<byte> data)
    {
        var address = (ushort)(data[1] << 8 | data[2]);
        var value = (ushort)(data[3] << 8 | data[4]);

        await Task.Run(() => _master!.WriteSingleRegister(1, address, value));
        return BitConverter.GetBytes(value);
    }

    private async Task<byte[]> HandleWriteMultipleRegistersAsync(IReadOnlyList<byte> data)
    {
        var start = (ushort)(data[1] << 8 | data[2]);
        var count = (ushort)(data[3] << 8 | data[4]);
        var byteCount = data[5];

        var values = new ushort[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = (ushort)(data[6 + i * 2] << 8 | data[6 + i * 2 + 1]);
        }

        await Task.Run(() => _master!.WriteMultipleRegisters(1, start, values));
        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }
}