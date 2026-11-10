using System;

namespace ZtarVega.Services
{
    /// <summary>A ROM found while scanning: a loose file or one of the files stored inside a ZIP archive.</summary>
    internal sealed class ScannedRom
    {
        public ScannedRom(string entryName, string crc32)
        {
            EntryName = entryName;
            Crc32 = crc32 ?? throw new ArgumentNullException(nameof(crc32));
        }

        /// <summary>The name of the ROM inside the archive, or null when it is a loose file.</summary>
        public string EntryName { get; }

        public string Crc32 { get; }
    }
}