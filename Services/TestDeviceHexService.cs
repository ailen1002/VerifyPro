using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class TestDeviceHexService : ICommunicationService
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public bool IsConnected => _client?.Connected ?? false;

    public async Task<bool> ConnectAsync(string ip, int port)
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _stream?.Dispose();
        _client?.Close();
        _client?.Dispose();
        _stream = null;
        _client = null;
        await Task.CompletedTask;
    }

    public async Task<byte[]> SendAsync(byte[] data)
    {
        if (_stream == null)
            throw new InvalidOperationException("未连接");

        await _stream.WriteAsync(data, 0, data.Length);
        
        //接收数据暂不处理
        //var buffer = new byte[1024];
        //var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
        //return buffer.Take(bytesRead).ToArray();
        return [];
    }
}
 