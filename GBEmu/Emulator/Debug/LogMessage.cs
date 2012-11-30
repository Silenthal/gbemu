namespace GBEmu.Emulator.Debug
{
    using System;

    internal enum LogMessageSource
    {
        Video,
        CPU,
        Audio,
        Memory,
        Cart,
        Timer
    }

    internal class LogMessage
    {
        public LogMessageSource source;
        public string position;
        public string message;
        public long time;

        public override string ToString()
        {
            string src = "<" + source + ">";
            string tm = "[" + time + "]";
            string pos = String.IsNullOrEmpty(position) ? "" : "[" + position + "]";
            return src + tm + pos + message;
        }
    }
}