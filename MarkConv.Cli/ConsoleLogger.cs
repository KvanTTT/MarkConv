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

        public void Info(string message) => WriteMessage(message, true);

        public void Warn(string message) => WriteMessage(message, false);

        private void WriteMessage(string message, bool info)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Console.WriteLine($"[{(info ? "INFO" : "WARN")}] {message}");
            else
                Console.WriteLine();
        }
    }
}
