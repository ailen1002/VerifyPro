using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using Modbus.Device;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class ModbusRtuService : IModbusRtuClient
{
    private SerialPort? _serialPort;
    private byte _slaveId = 1;
    private IModbusSerialMaster? _master;

    public bool IsConnected => _serialPort?.IsOpen ?? false;

    public async Task<bool> ConnectAsync(string portName, int baudRate, byte slaveId)
    {
        _slaveId = slaveId;
        
        _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        _serialPort.Open();
        _master = ModbusSerialMaster.CreateRtu(_serialPort);
        return await Task.FromResult(true);
    }
    
    public async Task DisconnectAsync()
    {
        if (_serialPort is { IsOpen: true })
            _serialPort.Close();

        _serialPort?.Dispose();
        _serialPort = null;
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

        var values = await Task.Run(() => _master!.ReadHoldingRegisters(_slaveId, start, count));
        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }

    private async Task<byte[]> HandleReadInputRegistersAsync(IReadOnlyList<byte> data)
    {
        var start = (ushort)(data[1] << 8 | data[2]);
        var count = (ushort)(data[3] << 8 | data[4]);

        var values = await Task.Run(() => _master!.ReadInputRegisters(_slaveId, start, count));
        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }

    private async Task<byte[]> HandleWriteSingleRegisterAsync(IReadOnlyList<byte> data)
    {
        var address = (ushort)(data[1] << 8 | data[2]);
        var value = (ushort)(data[3] << 8 | data[4]);

        await Task.Run(() => _master!.WriteSingleRegister(_slaveId, address, value));
        return BitConverter.GetBytes(value);
    }

    private async Task<byte[]> HandleWriteMultipleRegistersAsync(IReadOnlyList<byte> data)
    {
        var start = (ushort)(data[1] << 8 | data[2]);
        var count = (ushort)(data[3] << 8 | data[4]);

        var values = new ushort[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = (ushort)(data[6 + i * 2] << 8 | data[6 + i * 2 + 1]);
        }

        await Task.Run(() => _master!.WriteMultipleRegisters(_slaveId, start, values));
        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }
}
