namespace GBEmu.Emulator.Timing
{
    public interface ITimekeeper
    {
        void Start();

        void Stop();

        double Duration();

        double ElapsedTime();
    }
}