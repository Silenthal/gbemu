using GBEmu.Emulator.Timing;

namespace GBEmu.Emulator.Debug
{
    public class LogMessage
    {
        public LogMessageSource source;
        public int position;
        public string message;
        public long time;

        public LogMessage()
        {
            source = LogMessageSource.Audio;
            time = GlobalTimer.GetInstance().GetTime();
            position = -1;
        }

        public LogMessage(LogMessageSource messageSource, string msg)
        {
            source = messageSource;
            message = msg;
        }

        public LogMessage(LogMessageSource messageSource, int readPosition, string msg)
            : this(messageSource, msg)
        {
            position = readPosition;
        }

        public override string ToString() => $"<{source}>[{time.ToString("D10")}]{(position < 0 ? "" : $"[{position:X4}]")}{message}";
    }
}