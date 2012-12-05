namespace GBEmu.Emulator.Input
{
    using GBEmu.Emulator;

    public interface IInputHandler
    {
        KeyState PollInput();
    }
}