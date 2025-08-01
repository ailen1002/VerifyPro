using System.Threading.Tasks;
using VerifyPro.Enums;

namespace VerifyPro.Services;

public class ModbusRtuOutputPort(RtuOutput output, ModbusRtuController board)
{
    public Task On() => board.WriteOutputAsync(output, true);
    public Task Off() => board.WriteOutputAsync(output, false);
}