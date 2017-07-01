using System;

namespace HabraMark
{
    public class ConsoleLogger : ILogger
    {
        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }
    }
}
