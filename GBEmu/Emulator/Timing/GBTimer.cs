using GBEmu.Emulator.Debug;
using GBEmu.Emulator.IO;

namespace GBEmu.Emulator.Timing
{
    public class GBTimer : TimedIODevice
    {
        private Divider divider;
        private TimerCounter timerCounter;

        public GBTimer(InterruptManager iM)
        {
            CycleCounter = 0;
            divider = new Divider();
            timerCounter = new TimerCounter(iM);
        }

        public override byte Read(int position)
        {
            switch (position & 0xFF)
            {
                case IOPorts.DIV:
                    return divider.Read(position);

                case IOPorts.TIMA:
                case IOPorts.TMA:
                case IOPorts.TAC:
                    return timerCounter.Read(position);

                default:
                    Logger.GetInstance().Log(new LogMessage()
                    {
                        source = LogMessageSource.Timer,
                        time = GlobalTimer.GetInstance().GetTime(),
                        position = position.ToString("X4"),
                        message = "Failed read."
                    });
                    return 0xFF;
            }
        }

        public override void Write(int position, byte data)
        {
            switch (position & 0xFF)
            {
                case IOPorts.DIV:
                    divider.Write(position, data);
                    break;

                case IOPorts.TIMA:
                case IOPorts.TMA:
                case IOPorts.TAC:
                    timerCounter.Write(position, data);
                    break;

                default:
                    Logger.GetInstance().Log(new LogMessage()
                    {
                        source = LogMessageSource.Timer,
                        time = GlobalTimer.GetInstance().GetTime(),
                        position = position.ToString("X4"),
                        message = "Failed write."
                    });
                    break;
            }
        }

        public override void UpdateTime(int cycles)
        {
            divider.UpdateTime(cycles);
            timerCounter.UpdateTime(cycles);
        }
    }
}