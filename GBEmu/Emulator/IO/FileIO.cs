namespace GBEmu.Emulator.IO
{
    internal static class FileIO
    {
        public static byte[] LoadFile(string path)
        {
            return System.IO.File.ReadAllBytes(path);
        }

        public static bool Exists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public static string GetPathWithDifferentExtension(string path, string extension)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), System.IO.Path.GetFileNameWithoutExtension(path) + extension);
        }
    }
}