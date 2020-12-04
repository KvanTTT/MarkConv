using System.Collections.Generic;

namespace MarkConv
{
    public class Logger : ILogger
    {
        public List<string> InfoMessages { get; }

        public List<string> WarningMessages { get; }

        public List<string> ErrorMessages { get; }

        public Logger()
        {
            InfoMessages = new List<string>();
            WarningMessages = new List<string>();
            ErrorMessages = new List<string>();
        }

        public void Info(string message)
        {
            lock (InfoMessages)
            {
                InfoMessages.Add(message);
            }
        }

        public void Warn(string message)
        {
            lock (WarningMessages)
            {
                WarningMessages.Add(message);
            }
        }

        public void Error(string message)
        {
            lock (ErrorMessages)
            {
                ErrorMessages.Add(message);
            }
        }

        public int ErrorCount => ErrorMessages.Count;

        public void Clear()
        {
            InfoMessages.Clear();
            WarningMessages.Clear();
            ErrorMessages.Clear();
        }
    }
}
