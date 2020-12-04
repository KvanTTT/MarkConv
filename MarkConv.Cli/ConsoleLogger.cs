using System;
using System.Text;

namespace MarkConv.Cli
{
    public enum MessageType
    {
        Info,
        Warn,
        Error
    }

    public class ConsoleLogger : ILogger
    {
        private int _errorCount;

        public ConsoleLogger()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        public void Info(string message) => WriteMessage(message, MessageType.Info);

        public void Warn(string message) => WriteMessage(message, MessageType.Warn);

        public void Error(string message)
        {
            WriteMessage(message, MessageType.Error);
            _errorCount++;
        }

        public int ErrorCount => _errorCount;

        private void WriteMessage(string message, MessageType messageType)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Console.WriteLine($"[{messageType.ToString().ToUpperInvariant()}] {message}");
            else
                Console.WriteLine();
        }
    }
}
