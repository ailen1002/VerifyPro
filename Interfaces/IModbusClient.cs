using System.Threading.Tasks;

namespace VerifyPro.Interfaces;

public interface IModbusClient
{
    Task<bool> ConnectAsync(string ip, int port);
    Task DisconnectAsync();
    Task<byte[]> SendAsync(byte[] data);
    bool IsConnected { get; }
}