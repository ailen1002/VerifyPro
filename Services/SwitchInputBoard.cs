using System;
using System.Threading.Tasks;
using VerifyPro.Interfaces;

namespace VerifyPro.Services;

public class SwitchInputBoard(ICommunicationService service)
{
    private readonly ICommunicationService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly bool[] _inputs = new bool[16];
    
    public async Task RefreshAsync()
    {
        const ushort quantity = 16;

        var command = new byte[]
        {
            0x03,
            0x00, 0x00,
            0x00, (byte)quantity
        };

        var response = await _service.SendAsync(command);

        if (response == null)
            throw new InvalidOperationException("响应为空");

        // 响应没有报文头，直接数据区，长度至少 quantity * 2
        if (response.Length < quantity * 2)
            throw new InvalidOperationException($"响应字节不足，期望至少{quantity * 2}字节，实际{response.Length}字节");

        for (var i = 0; i < quantity; i++)
        {
            var byteIndex = i * 2;
            if (byteIndex + 1 >= response.Length)
            {
                _inputs[i] = false;
                continue;
            }

            // 低字节在前
            var value = (ushort)((response[byteIndex + 1] << 8) | response[byteIndex]);
            _inputs[i] = value != 0;
        }
    }
    public bool di1 => _inputs[0];
    public bool di2 => _inputs[1];
    public bool di3 => _inputs[2];
    public bool di4 => _inputs[3];
    public bool di5 => _inputs[4];
    public bool di6 => _inputs[5];
    public bool di7 => _inputs[6];
    public bool di8 => _inputs[7];
    public bool di9 => _inputs[8];
    public bool di10 => _inputs[9];
    public bool di11 => _inputs[10];
    public bool di12 => _inputs[11];
    public bool di13 => _inputs[12];
    public bool di14 => _inputs[13];
    public bool di15 => _inputs[14];
    public bool di16 => _inputs[15];
}
