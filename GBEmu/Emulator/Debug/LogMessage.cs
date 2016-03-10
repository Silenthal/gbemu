using System;

namespace GBEmu.Emulator.Debug
{
    public class LogMessage
    {
        public LogMessageSource source;
        public string position;
        public string message;
        public long time;

        public LogMessage()
        {
            source = LogMessageSource.Audio;
        }

        public LogMessage(LogMessageSource messageSource, string msg)
        {
            source = messageSource;
            message = msg;
        }

        public LogMessage(LogMessageSource messageSource, long messageTime, string msg)
            : this(messageSource, msg)
        {
            time = messageTime;
        }

        public LogMessage(LogMessageSource messageSource, long messageTime, string readPosition, string msg)
            : this(messageSource, messageTime, msg)
        {
            position = readPosition;
        }

        public override string ToString()
        {
            string src = "<" + source + ">";
            string tm = "[" + time + "]";
            string pos = String.IsNullOrEmpty(position) ? "" : "[" + position + "]";
            return src + tm + pos + message;
        }
    }
}