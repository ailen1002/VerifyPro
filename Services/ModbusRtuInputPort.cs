using System.Threading.Tasks;
using VerifyPro.Enums;

namespace VerifyPro.Services;

public class ModbusRtuInputPort (RtuInput input, ModbusRtuController board)
{
    public Task Read() => board.ReadInputAsync(input);
}