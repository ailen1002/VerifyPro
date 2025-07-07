using System.Threading.Tasks;
using VerifyPro.Enums;

namespace VerifyPro.Services;

public class RelayOutputPort(RelayOutput output, RelayOutputBoard board)
{
    public Task On() => board.WriteOutputAsync(output, true);
    public Task Off() => board.WriteOutputAsync(output, false);
}
