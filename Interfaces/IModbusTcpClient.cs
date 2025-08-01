using System.Threading.Tasks;

namespace VerifyPro.Interfaces;

public interface IModbusTcpClient
{
    Task<bool> ConnectAsync(string ip, int port);
    Task DisconnectAsync();
    Task<byte[]> SendAsync(byte[] data);
    bool IsConnected { get; }
}