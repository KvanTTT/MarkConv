using System;
using System.Collections.Generic;

namespace HabraMark
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

        public void LogInfo(string message)
        {
            InfoMessages.Add(message);
        }

        public void LogWarning(string message)
        {
            WarningMessages.Add(message);
        }
    }
}
