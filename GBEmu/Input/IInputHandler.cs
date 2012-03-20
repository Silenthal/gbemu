using GBEmu.Emulator;

namespace GBEmu.Input
{
	interface IInputHandler
	{
		void PollInput(GBSystem system);
	}
}
