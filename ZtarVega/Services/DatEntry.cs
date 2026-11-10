using System;

namespace ZtarVega.Services
{
    /// <summary>One game of a DAT file: its name, category, checksum and its place in the family of versions.</summary>
    internal sealed class DatEntry
    {
        public DatEntry(string name, string category, string crc32, string datId, string parentDatId)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category;
            Crc32 = crc32 ?? throw new ArgumentNullException(nameof(crc32));
            DatId = datId;
            ParentDatId = parentDatId;
        }

        public string Name { get; }

        /// <summary>The category written in the DAT file (for example "Games"), or null when it has none.</summary>
        public string Category { get; }

        /// <summary>The CRC32 as 8 uppercase hexadecimal digits.</summary>
        public string Crc32 { get; }

        /// <summary>The id the DAT file gives to this entry, or null when it has none.</summary>
        public string DatId { get; }

        /// <summary>The id of the main version this entry is a clone of (another region or a revision), or null.</summary>
        public string ParentDatId { get; }
    }
}