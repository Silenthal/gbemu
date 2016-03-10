namespace GBEmu.Emulator.Audio
{
    using GBEmu.Emulator.IO;

    internal class GBAudio : TimedIODevice
    {

        public GBAudio()
        {
        }

        public override byte Read(int position)
        {
            return 0xFF;
        }

        public override void Write(int position, byte data)
        {
        }

        public override void UpdateTime(int cycles)
        {
            // Depending on time...

        }
    }
}