namespace GBEmu.Emulator.Input
{
    public interface IInputHandler
    {
        KeyState PollInput();
    }
}