namespace GBEmu.Emulator.Timing
{
    public interface ITimekeeper
    {
        void Start();

        void Restart();

        void Stop();

        double ElapsedSeconds();
    }
}