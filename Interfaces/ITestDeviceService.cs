using System.Threading.Tasks;

namespace VerifyPro.Interfaces;

public interface ITestDeviceService
{
    Task<bool> ConnectAsync(string ip, int port);
    Task DisconnectAsync();
    Task<byte[]> SendAsync(byte[] data);
    Task<bool> SystemStop(byte[] data);
    Task<byte[]> AnalogDetection(byte[] data);
    bool IsConnected { get; }
}