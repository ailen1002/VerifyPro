using System;
using System.Threading.Tasks;
using VerifyPro.Enums;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class ModbusRtuController
{
    private readonly IModbusRtuClient _service;
    
    // 输出控制器对象
    public ModbusRtuOutputPort Forward { get; }
    public ModbusRtuOutputPort Reverse { get; }
    public ModbusRtuOutputPort VoltageSwitch { get; }
    public ModbusRtuOutputPort RemoteControlA { get; }
    public ModbusRtuOutputPort RemoteControlB { get; }
    public ModbusRtuOutputPort CapacitorDischargeA { get; }
    public ModbusRtuOutputPort CapacitorDischargeB { get; }
    public ModbusRtuOutputPort CapacitorDischargeC { get; }
    
    // 输入控制器对象
    public ModbusRtuInputPort Ac200Forward { get; }
    public ModbusRtuInputPort Ac380Forward { get; }
    public ModbusRtuController(IModbusRtuClient service)
    {
        _service = service;

        // 为每个输出创建对应的控制对象
        Forward = CreateOutputPort(RtuOutput.Forward);
        Reverse = CreateOutputPort(RtuOutput.Reverse);
        VoltageSwitch = CreateOutputPort(RtuOutput.VoltageSwitch);
        RemoteControlA = CreateOutputPort(RtuOutput.RemoteControlA);
        RemoteControlB = CreateOutputPort(RtuOutput.RemoteControlB);
        CapacitorDischargeA = CreateOutputPort(RtuOutput.CapacitorDischargeA);
        CapacitorDischargeB = CreateOutputPort(RtuOutput.CapacitorDischargeB);
        CapacitorDischargeC = CreateOutputPort(RtuOutput.CapacitorDischargeC);
        
        // 为每个输入创建对应的的控制对象
        Ac200Forward = CreateInputPort(RtuInput.Ac200Forward);
        Ac380Forward = CreateInputPort(RtuInput.Ac380Forward);
    }
    

    private ModbusRtuOutputPort CreateOutputPort(RtuOutput output)
        => new ModbusRtuOutputPort(output, this);

    private ModbusRtuInputPort CreateInputPort(RtuInput input)
        => new ModbusRtuInputPort(input, this);
    
    public async Task WriteOutputAsync(RtuOutput output, bool on)
    {
        var address = (ushort)output;
        var value = (ushort)(on ? 1 : 0);

        var command = new byte[]
        {
            0x06,
            (byte)(address >> 8), (byte)(address & 0xFF),
            (byte)(value >> 8), (byte)(value & 0xFF)
        };
        
        var response = await _service.SendAsync(command);
    }

    public async Task<bool> ReadInputAsync(RtuInput input)
    {
        var address = (ushort)input;
        var command = new byte[]
        {
            0x04,
            (byte)(address >> 8),
            (byte)(address & 0xFF),
            0x00, 0x01
        };

        var response = await _service.SendAsync(command);
        
        var value = (ushort)(response[3] << 8 | response[4]);
        
        return value != 0;
    }
}