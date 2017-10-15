using System;
using System.Text;

namespace MarkConv
{
    public class ConsoleLogger : ILogger
    {
        public ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public void Info(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public void Warn(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }
    }
}
