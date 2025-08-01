using System;
using System.Threading.Tasks;
using VerifyPro.Enums;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class RelayOutputBoard
{
    private readonly IModbusTcpClient _service;

    public RelayOutputBoard(IModbusTcpClient service)
    {
        _service = service;

        // 为每个输出创建对应的控制对象
        ApSwitch = CreatePort(RelayOutput.ApSwitch);
        TestSwitch = CreatePort(RelayOutput.TestSwitch);
        SnowSwitch = CreatePort(RelayOutput.SnowSwitch);
        SilentSwitch = CreatePort(RelayOutput.SilentSwitch);
        Drm1Switch = CreatePort(RelayOutput.Drm1Switch);
        Drm2Switch = CreatePort(RelayOutput.Drm2Switch);
        ForcedStopSwitch = CreatePort(RelayOutput.ForcedStopSwitch);
        NumCompSwitch = CreatePort(RelayOutput.NumCompSwitch);
        Boot = CreatePort(RelayOutput.Boot);
        UbSwitch = CreatePort(RelayOutput.UbSwitch);
        MiconResetSwitch = CreatePort(RelayOutput.MiconResetSwitch);
        HicSwitch = CreatePort(RelayOutput.HicSwitch);
        HighPressureSwitch = CreatePort(RelayOutput.HighPressureSwitch);
    }

    private RelayOutputPort CreatePort(RelayOutput output)
        => new RelayOutputPort(output, this);

    public RelayOutputPort ApSwitch { get; }
    public RelayOutputPort TestSwitch { get; }
    public RelayOutputPort SnowSwitch { get; }
    public RelayOutputPort SilentSwitch { get; }
    public RelayOutputPort Drm1Switch { get; }
    public RelayOutputPort Drm2Switch { get; }
    public RelayOutputPort ForcedStopSwitch { get; }
    public RelayOutputPort NumCompSwitch { get; }
    public RelayOutputPort Boot { get; }
    public RelayOutputPort UbSwitch { get; }
    public RelayOutputPort MiconResetSwitch { get; }
    public RelayOutputPort HicSwitch { get; }
    public RelayOutputPort HighPressureSwitch { get; }

    public async Task WriteOutputAsync(RelayOutput output, bool on)
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
}