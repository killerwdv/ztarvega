using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace ZtarVega.Services
{
    /// <summary>Computes the CRC32 checksum of ROM files.</summary>
    internal static class Crc32Calculator
    {
        private const uint Polynomial = 0xEDB88320;
        private const int BufferSize = 64 * 1024;
        private const int InesHeaderSize = 16;

        // Every iNES file starts with the bytes "NES" followed by 0x1A.
        private static readonly byte[] InesSignature = { 0x4E, 0x45, 0x53, 0x1A };

        private static readonly uint[] Table = CreateTable();

        /// <summary>
        /// Returns the CRC32 of the ROM data of a file as 8 uppercase hexadecimal digits. When the file has an
        /// iNES header, the 16 header bytes are not included, so the checksum matches the ones published
        /// by ROM preservation databases and does not change when only the header is edited.
        /// </summary>
        public static string ComputeRomChecksum(string path, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                return ComputeRomChecksum(stream, cancellationToken);
            }
        }

        /// <summary>The same calculation for a stream, which must be seekable. It is read from its beginning.</summary>
        public static string ComputeRomChecksum(Stream stream, CancellationToken cancellationToken)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            SkipInesHeaderIfPresent(stream);

            uint crc = 0xFFFFFFFF;
            var buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                for (int index = 0; index < bytesRead; index++)
                {
                    crc = Table[(crc ^ buffer[index]) & 0xFF] ^ (crc >> 8);
                }
            }

            return (~crc).ToString("X8", CultureInfo.InvariantCulture);
        }

        private static void SkipInesHeaderIfPresent(Stream stream)
        {
            stream.Position = 0;

            if (stream.Length < InesHeaderSize)
            {
                return;
            }

            var signature = new byte[InesSignature.Length];
            int totalRead = 0;

            while (totalRead < signature.Length)
            {
                int read = stream.Read(signature, totalRead, signature.Length - totalRead);

                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            bool hasHeader = totalRead == signature.Length && signature.SequenceEqual(InesSignature);
            stream.Position = hasHeader ? InesHeaderSize : 0;
        }

        private static uint[] CreateTable()
        {
            var table = new uint[256];

            for (uint index = 0; index < table.Length; index++)
            {
                uint value = index;

                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0 ? (value >> 1) ^ Polynomial : value >> 1;
                }

                table[index] = value;
            }

            return table;
        }
    }
}