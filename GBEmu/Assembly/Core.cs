using System;
using System.IO;

namespace GBRead
{
    
    public class Core
    {
        private byte[] romFile;
        public byte[] ROMFile { get { return romFile; } }
        public bool FileLoaded { get; private set; }

        private static int MAX_ROM_SIZE = 0x400000;
        private static int MIN_ROM_SIZE = 0x8000;
        
        public Core()
        {
            FileLoaded = false;
        }

        public Core(byte[] rFile)
        {
            LoadFile(rFile);
        }

        public bool LoadFile(byte[] rFile)
        {
            if (rFile.Length < MAX_ROM_SIZE && rFile.Length >= MIN_ROM_SIZE)
            {
                romFile = new byte[rFile.Length];
                Array.Copy(rFile, romFile, romFile.Length);
                FileLoaded = true;
            }
            else FileLoaded = false;
            return FileLoaded;
        }

        public bool ModifyFile(int offset, byte[] insertFile)
        {
            if (!FileLoaded || offset < 0 || offset + insertFile.Length >= romFile.Length)
            {
                return false;
            }
            else
            {
                Array.Copy(insertFile, 0, romFile, offset, insertFile.Length);
                return true;
            }
        }

        public bool SaveFile(string newFileName)
        {
            if (romFile.Length > 0)
            {
                FixHeader();
                File.WriteAllBytes(newFileName, romFile);
                return true;
            }
            else return false;
        }

        public int GetCompCheck()
        {
            if (FileLoaded)
            {
                byte compCheck = 0;
                for (int i = 0x134; i < 0x14D; i++)
                {
                    byte x = romFile[i];
                    compCheck -= ++x;
                }
                return compCheck;
            }
            else return Int32.MinValue;
        }

        public int GetCheckSum()
        {
            if (FileLoaded)
            {
                ushort checksum = 0;
                foreach (Byte bt in ROMFile)
                {
                    checksum += bt;
                }
                checksum -= romFile[0x14D];
                checksum -= romFile[0x14E];
                checksum -= romFile[0x14F];
                checksum += (byte)GetCompCheck();
                return checksum;
            }
            else return Int32.MinValue;
        }

        public void FixHeader()
        {
            int compCheck = GetCompCheck();
            int checksum = GetCheckSum();
            if (compCheck != Int32.MinValue && checksum != Int32.MinValue)
            {
                ModifyFile(0x14D, new byte[] { (byte)compCheck });
                ModifyFile(0x14E, new byte[] { (byte)(checksum >> 8), (byte)checksum });
            }
        }

        public byte ReadByte(int offset)
        {
            return offset < romFile.Length ? romFile[offset] : (byte)0;
        }

        public int ReadWord(int offset)
        {
            if (offset > 0 && offset < romFile.Length - 1)
            {
                return (ushort)(romFile[offset] | (romFile[offset + 1] << 8));
            }
            else return Int32.MinValue;
        }

        public static byte ReadByteFromFile(byte[] refFile, int offset)
        {
            if (offset > 0 && offset < refFile.Length)
            {
                return refFile[offset];
            }
            else return 0;
        }

        public static int ReadWordFromFile(byte[] refFile, int offset)
        {
            if (offset < 0 || offset > refFile.Length - 1) return Int32.MinValue;
            else return (ushort)((refFile[offset + 1] << 8) + refFile[offset]);
        }
    }
}
