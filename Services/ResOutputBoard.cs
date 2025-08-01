using System;
using System.Linq;
using System.Threading.Tasks;
using VerifyPro.Enums;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class ResOutputBoard(IModbusTcpClient service)
{
    public async Task CloseOddChannels()
    {
        await CloseAll(false);
        await ApplyToChannels((index, _) => index % 2 == 1, true);
    }

    public async Task CloseEvenChannels()
    {
        await CloseAll(false);
        await ApplyToChannels((index, _) => index % 2 == 0, true);
    }

    public async Task CloseAllChannels()
    {
        await ApplyToChannels((_, _) => true, true);
    }
    
    public async Task OpenAllChannels()
    {
        await ApplyToChannels((_, _) => true, false);
    }
    
    private async Task CloseAll(bool state)
    {
        await ApplyToChannels((_, _) => true, state);
    }

    private async Task ApplyToChannels(Func<int, ResOutput, bool> condition, bool on)
    {
        var values = Enum.GetValues(typeof(ResOutput)).Cast<ResOutput>().ToList();

        for (var i = 0; i < values.Count; i++)
        {
            var output = values[i];
            if (condition(i, output))
            {
                await WriteOutputAsync(output, on);
            }
        }
    }

    private async Task WriteOutputAsync(ResOutput output, bool on)
    {
        var address = (ushort)output;
        var value = (ushort)(on ? 1 : 0);

        byte[] command =
        [
            0x06,
            (byte)(address >> 8), (byte)(address & 0xFF),
            (byte)(value >> 8), (byte)(value & 0xFF)
        ];
        
        var response = await service.SendAsync(command);
    }
}