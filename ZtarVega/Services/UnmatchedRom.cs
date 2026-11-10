using System;

namespace ZtarVega.Services
{
    /// <summary>A ROM found during a scan that does not correspond to any game of the catalog.</summary>
    internal sealed class UnmatchedRom
    {
        public UnmatchedRom(string fileName, string crc32, string entryName = null)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Crc32 = crc32 ?? throw new ArgumentNullException(nameof(crc32));
            EntryName = entryName;
        }

        /// <summary>The file in the ROMs folder: the ROM itself, or the ZIP archive that holds it.</summary>
        public string FileName { get; }

        public string Crc32 { get; }

        /// <summary>The name of the ROM inside the archive, or null when the ROM is a loose file.</summary>
        public string EntryName { get; }

        /// <summary>The name a new game is named after: the ROM inside the archive when there is one, otherwise the file.</summary>
        public string NameForTitle
        {
            get { return EntryName ?? FileName; }
        }
    }
}