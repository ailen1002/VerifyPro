using System;
using System.Threading.Tasks;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class VoltageInputBoard(ICommunicationService service)
{
    private readonly ICommunicationService _service = service ?? throw new ArgumentNullException(nameof(service));

    public ushort[] Gain { get; } = new ushort[16];
    public ushort[] Offset { get; } = new ushort[16];
    public ushort[] AdValues { get; } = new ushort[16];
    public float[] Voltages { get; } = new float[16];

    public async Task ReadVoltageAsync()
    {
        const ushort startAddress = 48;
        const ushort quantity = 16;

        var command = new byte[]
        {
            0x03,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };

        var response = await _service.SendAsync(command);

        if (response == null)
            throw new InvalidOperationException("响应为空");

        if (response.Length < quantity * 2)
            throw new InvalidOperationException($"响应长度不足，预期至少 {quantity * 2} 字节，实际 {response.Length} 字节");

        for (var i = 0; i < 16; i++)
        {
            var byteIndex = i * 2;

            var value = (ushort)((response[byteIndex + 1] << 8) | response[byteIndex]);

            Voltages[i] = (float)Math.Round(value / 10.0f, 1);
        }
    }

    public async Task WriteGainOffsetAsync()
    {
        const ushort startAddress = 0;
        const ushort quantity = 32;
        
        var values = new ushort[32];
        for (var i = 0; i < 16; i++)
            values[i] = 463;
        for (var i = 16; i < 32; i++)
            values[i] = unchecked((ushort)-3);
        
        const byte byteCount = (byte)(quantity * 2);
        var command = new byte[7 + byteCount];

        command[0] = 0x10;
        command[1] = (byte)(startAddress >> 8);
        command[2] = (byte)(startAddress & 0xFF);
        command[3] = (byte)(quantity >> 8);
        command[4] = (byte)(quantity & 0xFF);
        command[5] = byteCount;
        
        for (var i = 0; i < quantity; i++)
        {
            command[6 + i * 2 + 1] = (byte)(values[i] & 0xFF);
            command[6 + i * 2] = (byte)((values[i] >> 8) & 0xFF);
        }
        
        var response = await _service.SendAsync(command);
        
        if (response == null || response.Length < 6)
            throw new InvalidOperationException("Modbus 写入失败或响应无效");
    }
    
    public float Volt1 => Voltages[0];
    public float Volt2 => Voltages[1];
    public float Volt3 => Voltages[2];
    public float Volt4 => Voltages[3];
    public float Volt5 => Voltages[4];
    public float Volt6 => Voltages[5];
    public float Volt7 => Voltages[6];
    public float Volt8 => Voltages[7];
    public float Volt9 => Voltages[8];
    public float Volt10 => Voltages[9];
    public float Volt11 => Voltages[10];
    public float Volt12 => Voltages[11];
    public float Volt13 => Voltages[12];
    public float Volt14 => Voltages[13];
    public float Volt15 => Voltages[14];
    public float Volt16 => Voltages[15];
}
