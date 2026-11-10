using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using ZtarVega.Localization;

namespace ZtarVega.Services
{
    /// <summary>Reads the ROMs stored inside ZIP archives.</summary>
    internal static class ZipRomReader
    {
        public const string ArchiveExtension = ".zip";

        private const string RomEntryExtension = ".nes";

        // NES ROMs are small. The limit protects against corrupt or malicious archives that claim huge sizes.
        private const long MaxRomSizeInBytes = 16L * 1024 * 1024;
        private const int CopyBufferSize = 64 * 1024;

        public static bool IsArchive(string path)
        {
            return string.Equals(Path.GetExtension(path), ArchiveExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns every ROM of the archive with its checksum. Throws <see cref="InvalidDataException"/>
        /// when the archive is damaged.
        /// </summary>
        public static IReadOnlyList<ScannedRom> ReadRoms(string archivePath, CancellationToken cancellationToken)
        {
            var roms = new List<ScannedRom>();

            using (var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries.Where(IsRomEntry))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (MemoryStream data = ReadEntry(entry))
                    {
                        roms.Add(new ScannedRom(entry.Name, Crc32Calculator.ComputeRomChecksum(data, cancellationToken)));
                    }
                }
            }

            return roms;
        }

        /// <summary>
        /// Writes to <paramref name="destinationPath"/> the ROM of the archive whose checksum is
        /// <paramref name="crc32"/> (the first ROM when no checksum is given). Returns false when the archive
        /// has no such ROM. Throws <see cref="InvalidDataException"/> when the archive is damaged.
        /// </summary>
        public static bool TryExtract(string archivePath, string crc32, string destinationPath)
        {
            using (var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries.Where(IsRomEntry))
                {
                    using (MemoryStream data = ReadEntry(entry))
                    {
                        bool isTheRom = string.IsNullOrEmpty(crc32) ||
                                        string.Equals(
                                            Crc32Calculator.ComputeRomChecksum(data, CancellationToken.None),
                                            crc32,
                                            StringComparison.OrdinalIgnoreCase);

                        if (!isTheRom)
                        {
                            continue;
                        }

                        // The file is written under a temporary name and then renamed, so an interrupted
                        // extraction never leaves a half-written ROM behind.
                        string temporaryPath = destinationPath + ".tmp";
                        data.Position = 0;

                        using (var output = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            data.CopyTo(output, CopyBufferSize);
                        }

                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }

                        File.Move(temporaryPath, destinationPath);
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsRomEntry(ZipArchiveEntry entry)
        {
            return entry.Length > 0 &&
                   entry.Length <= MaxRomSizeInBytes &&
                   entry.Name.EndsWith(RomEntryExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Reads a whole entry into memory, refusing entries that turn out to be larger than the limit.</summary>
        private static MemoryStream ReadEntry(ZipArchiveEntry entry)
        {
            var buffer = new MemoryStream((int)entry.Length);

            using (Stream source = entry.Open())
            {
                var chunk = new byte[CopyBufferSize];
                int read;

                while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
                {
                    if (buffer.Length + read > MaxRomSizeInBytes)
                    {
                        throw new InvalidDataException(Localizer.Get("zip.error.entryTooLarge"));
                    }

                    buffer.Write(chunk, 0, read);
                }
            }

            buffer.Position = 0;
            return buffer;
        }
    }
}