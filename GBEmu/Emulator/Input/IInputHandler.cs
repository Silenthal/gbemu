namespace GBEmu.Emulator.Input
{
    using GBEmu.Emulator;

    public interface IInputHandler
    {
        void PollInput(GBSystem system);
    }
}