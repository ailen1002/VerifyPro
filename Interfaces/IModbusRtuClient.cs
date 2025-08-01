using System.Threading.Tasks;

namespace VerifyPro.Interfaces;

public interface IModbusRtuClient
{
    Task<bool> ConnectAsync(string portName, int baudRate, byte slaveId);
    Task DisconnectAsync();
    Task<byte[]> SendAsync(byte[] data);
    bool IsConnected { get; }
}