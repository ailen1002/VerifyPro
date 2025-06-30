using System.Threading.Tasks;

namespace VerifyPro.Services;

public abstract class CommunicationService
{
    public async Task<string> SendCommandAsync(string command)
    {
        // 模拟通信
        await Task.Delay(500);
        return "OK"; // 真实项目中返回解析后的结果
    }
}