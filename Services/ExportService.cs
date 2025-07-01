using System;
using System.IO;
using System.Text;

namespace VerifyPro.Services
{
    public class ExportService
    {
        /// <summary>
        /// 从 DetectLog 字符串中解析并导出为 CSV 文件
        /// </summary>
        /// <param name="detectLog">完整检测日志文本</param>
        /// <param name="path">导出路径</param>
        public void ExportToCsv(string detectLog, string path)
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
            writer.WriteLine("日志"); // CSV 表头

            var lines = detectLog?
                            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) 
                        ?? [];

            foreach (var line in lines)
            {
                Console.WriteLine($"Log: {line}"); // 控制台打印调试
                writer.WriteLine(line);
            }
        }
    }
}