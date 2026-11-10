using System;
using System.Collections.Generic;

namespace ZtarVega.Services
{
    /// <summary>One game of a metadata file: its name and the fields the file provides for it.</summary>
    internal sealed class MetadataEntry
    {
        public MetadataEntry(string name, string crc32, IReadOnlyDictionary<string, string> fields)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Crc32 = crc32;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        public string Name { get; }

        /// <summary>The checksum written in the file, or null. It is not used for matching: these checksums come from another dump flavor.</summary>
        public string Crc32 { get; }

        /// <summary>The fields of the entry, such as "developer", "genre" or "releaseyear".</summary>
        public IReadOnlyDictionary<string, string> Fields { get; }
    }
}