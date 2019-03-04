using System.Collections.Generic;

namespace MarkConv
{
    public class Logger : ILogger
    {
        public List<string> InfoMessages { get; }
        public List<string> WarningMessages { get; }

        public Logger()
        {
            InfoMessages = new List<string>();
            WarningMessages = new List<string>();
        }

        public void Info(string message)
        {
            InfoMessages.Add(message);
        }

        public void Warn(string message)
        {
            WarningMessages.Add(message);
        }

        public void Clear()
        {
            InfoMessages.Clear();
            WarningMessages.Clear();
        }
    }
}
