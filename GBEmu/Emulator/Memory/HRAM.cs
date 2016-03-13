using GBEmu.Emulator.IO;

namespace GBEmu.Emulator.Memory
{
    /// <summary>
    /// [FF80-FFFE] Represents the High RAM contained in the system.
    /// </summary>
    public class HRAM : IReadWriteCapable
    {
        private byte[] hram = new byte[0x7F];

        public byte Read(int position)
        {
            if (position >= 0xFF80 && position < 0xFFFF)
            {
                return hram[position - 0xFF80];
            }
            else
            {
                return 0xFF;
            }
        }

        public void Write(int position, byte data)
        {
            if (position >= 0xFF80 && position < 0xFFFF)
            {
                hram[position - 0xFF80] = data;
            }
        }
    }
}