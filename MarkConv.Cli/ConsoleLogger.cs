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
        private static readonly object LockObject = new object();
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
            lock (LockObject)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var oldColor = Console.ForegroundColor;
                    if (messageType == MessageType.Warn)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (messageType == MessageType.Error)
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{messageType.ToString().ToUpperInvariant()}] {message}");
                    Console.ForegroundColor = oldColor;
                }
                else
                    Console.WriteLine();
            }
        }
    }
}
