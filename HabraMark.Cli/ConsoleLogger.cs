using System;

namespace HabraMark
{
    public class ConsoleLogger : ILogger
    {
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
