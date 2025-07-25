using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using VerifyPro.Interfaces;
using VerifyPro.Models;

namespace VerifyPro.Services;

public class TestDeviceHexService : ITestDeviceService
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

        await _stream.WriteAsync(data);

        var buffer = new byte[1024];
        var bytesRead = await _stream.ReadAsync(buffer);
        var response = buffer.Take(bytesRead).ToArray();

        return response;
    }
    
    public async Task<CommandResult> SetTxCommand(byte[] data, int expectedLength, string commandName = "")
    {
        if (_stream == null)
            throw new InvalidOperationException("未连接");

        await ClearStreamBufferAsync(_stream);
        
        await _stream.WriteAsync(data);

        var response = new byte[expectedLength];
        var totalRead = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            while (totalRead < expectedLength)
            {
                var bytesRead = await _stream.ReadAsync(response.AsMemory(totalRead, expectedLength - totalRead), cts.Token);
                if (bytesRead == 0)
                    break;

                totalRead += bytesRead;
            }
        }
        catch (OperationCanceledException)
        {
            return new CommandResult
            {
                CommandName = commandName,
                Success = false,
                Response = response,
                ActualLength = totalRead
            };
        }

        if (totalRead != expectedLength)
        {
            return new CommandResult
            {
                CommandName = commandName,
                Success = false,
                Response = response,
                ActualLength = totalRead
            };
        }

        byte checksum = 0x00;
        for (var i = 0; i < expectedLength - 1; i++)
            checksum ^= response[i];

#if DEBUG
        Console.WriteLine($"[{commandName}] 响应 HEX:");
        PrintHexWithLineBreaks(response, expectedLength);
        Console.WriteLine($"最终计算校验: {checksum:X2}");
        Console.WriteLine($"实际校验位: {response[^1]:X2}");
#endif

        return new CommandResult
        {
            CommandName = commandName,
            Success = checksum == response[expectedLength - 1],
            Response = response,
            ActualLength = totalRead
        };
    }


    public async Task<bool> SystemStop(byte[] data)
    {
        if (_stream == null)
            throw new InvalidOperationException("未连接");

        await _stream.WriteAsync(data);
        const int expectedLength = 15;
        var response = new byte[expectedLength];
        var totalRead = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            while (totalRead < 15)
            {
                var bytesRead = await _stream.ReadAsync(response.AsMemory(totalRead, 15 - totalRead), cts.Token);
                if (bytesRead == 0)
                    break;

                totalRead += bytesRead;
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (totalRead != 15)
            return false;

        byte checksum = 0x00;
        for (var i = 0; i < 14; i++)
            checksum ^= response[i];
#if DEBUG
        Console.WriteLine("响应 HEX:");
        PrintHexWithLineBreaks(response, 15);
        Console.WriteLine($"最终计算校验: {checksum:X2}");
        Console.WriteLine($"实际校验位: {response[^1]:X2}");
#endif
        return checksum == response[expectedLength - 1];
    }
    
    public async Task<bool> EepromInitial(byte[] data)
    {
        if (_stream == null)
            throw new InvalidOperationException("未连接");

        await _stream.WriteAsync(data);
        const int expectedLength = 14;
        var response = new byte[expectedLength];
        var totalRead = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            while (totalRead < 14)
            {
                var bytesRead = await _stream.ReadAsync(response.AsMemory(totalRead, 14 - totalRead), cts.Token);
                if (bytesRead == 0)
                    break;

                totalRead += bytesRead;
            }
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (totalRead != 15)
            return false;

        byte checksum = 0x00;
        for (var i = 0; i < 14; i++)
            checksum ^= response[i];
#if DEBUG
        Console.WriteLine($"响应 HEX: {response}");
#endif
        return checksum == response[expectedLength - 1];
    }
    
    public async Task<byte[]> AnalogDetection(byte[] command)
    {
        if (_stream == null)
            throw new InvalidOperationException("未连接");

        await _stream.WriteAsync(command);

        const int expectedLength = 62; // 62字节固定长度
        var response = new byte[expectedLength];
        var totalRead = 0;
        var timeout = TimeSpan.FromSeconds(2);
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            while (totalRead < expectedLength)
            {
                var bytesRead = await _stream.ReadAsync(response.AsMemory(totalRead, expectedLength - totalRead), cts.Token);
                if (bytesRead == 0)
                    break;
                totalRead += bytesRead;
            }
        }
        catch (OperationCanceledException)
        {
            // 超时
            return Array.Empty<byte>();
        }

        if (totalRead != expectedLength)
            return Array.Empty<byte>();

        // XOR 校验，异或前 61 字节，结果应等于最后一字节
        byte checksum = 0x00;
        for (var i = 0; i < expectedLength - 1; i++)
        {
            checksum ^= response[i];
        }
#if DEBUG
        Console.WriteLine("响应 HEX:");
        PrintHexWithLineBreaks(response, 15);
        Console.WriteLine($"最终计算校验: {checksum:X2}");
        Console.WriteLine($"实际校验位: {response[^1]:X2}");
#endif
        //return response;
        return checksum != response[expectedLength - 1] ? Array.Empty<byte>() : response;
    }
    
    private static void PrintHexWithLineBreaks(byte[] data, int bytesPerLine = 15)
    {
        for (var i = 0; i < data.Length; i += bytesPerLine)
        {
            var length = Math.Min(bytesPerLine, data.Length - i);
            var segment = new byte[length];
            Array.Copy(data, i, segment, 0, length);

            Console.WriteLine(BitConverter.ToString(segment));
        }
    }
    
    private async Task ClearStreamBufferAsync(Stream stream, int bufferSize = 1024, int clearTimeoutMs = 200)
    {
        if (!stream.CanRead)
            return;

        var buffer = new byte[bufferSize];
        var sw = Stopwatch.StartNew();

        while (sw.ElapsedMilliseconds < clearTimeoutMs)
        {
            if (stream is NetworkStream netStream)
            {
                if (!netStream.DataAvailable)
                    break;
            }

            else if (!stream.CanRead)
            {
                break;
            }
            
            if (stream.CanRead)
            {
                if (stream is NetworkStream { DataAvailable: false })
                    break;

                var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                var bytesRead = await Task.WhenAny(readTask, Task.Delay(50)) == readTask ? readTask.Result : 0;

                if (bytesRead == 0)
                    break;
            }
            else
            {
                break;
            }
        }
    }
}
 