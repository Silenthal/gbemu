using System;
using System.Collections.Generic;

namespace GBEmu.Emulator.Debug
{
    public class Logger
    {
        private static readonly Lazy<Logger> instance = new Lazy<Logger>(() => new Logger());
        private List<LogMessage> messageList = new List<LogMessage>();

        private Logger()
        {
        }

        public static Logger GetInstance()
        {
            return instance.Value;
        }

        public void Log(LogMessage message)
        {
            messageList.Add(message);
        }

        public List<LogMessage> GetMessages()
        {
            return messageList;
        }

        public void ClearMessages()
        {
            messageList.Clear();
        }
    }
}