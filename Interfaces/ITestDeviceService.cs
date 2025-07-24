using System.Threading.Tasks;
using VerifyPro.Models;

namespace VerifyPro.Interfaces;

public interface ITestDeviceService
{
    Task<bool> ConnectAsync(string ip, int port);
    Task DisconnectAsync();
    Task<byte[]> SendAsync(byte[] data);
    Task<CommandResult> SetTxCommand(byte[] data, int expectedLength, string commandName = "");
    Task<bool> SystemStop(byte[] data);
    Task<bool> EepromInitial(byte[] data);
    Task<byte[]> AnalogDetection(byte[] data);
    bool IsConnected { get; }
}