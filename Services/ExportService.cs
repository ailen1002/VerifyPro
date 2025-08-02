using System;
using System.IO;
using System.Text;

namespace VerifyPro.Services
{
    public class ExportService
    {
        public void ExportToCsv(string detectLog, string path)
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(true));
            writer.WriteLine("检测结果");

            var lines = detectLog?
                            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries) 
                        ?? [];

            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }
        }
    }
}