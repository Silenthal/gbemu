using GBEmu.Emulator.IO;

namespace GBEmu.Emulator.Memory
{
    /// <summary>
    /// [C000-DFFF] Represents the Work RAM contained in the system.
    /// </summary>
    public class WRAM : IReadWriteCapable
    {
        private byte[] wram = new byte[0x2000];
        
        public byte Read(int position)
        {
            if (position >= 0xC000 && position < 0xE000)
            {
                return wram[position - 0xC000];
            }
            else
            {
                return 0xFF;
            }
        }

        public void Write(int position, byte data)
        {
            if (position >= 0xC000 && position < 0xE000)
            {
                wram[position - 0xC000] = data;
            }
        }
    }
}