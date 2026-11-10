using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ZtarVega.Localization;

namespace ZtarVega.Services
{
    /// <summary>Reads the entries of a DAT file (the Logiqx XML format published by ROM preservation projects).</summary>
    internal static class DatFileReader
    {
        private static readonly Regex Crc32Pattern = new Regex("^[0-9A-Fa-f]{8}$", RegexOptions.Compiled);

        /// <summary>
        /// Reads every game that has a valid checksum. Throws <see cref="InvalidDataException"/> when the
        /// file is not a DAT file.
        /// </summary>
        public static IReadOnlyList<DatEntry> Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The DAT file path cannot be empty.", nameof(path));
            }

            var settings = new XmlReaderSettings
            {
                // A DOCTYPE is skipped, never resolved: a DAT file must not be able to read local files or start downloads.
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };

            var entries = new List<DatEntry>();

            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XmlReader reader = XmlReader.Create(stream, settings))
                {
                    reader.MoveToContent();

                    if (reader.NodeType != XmlNodeType.Element || reader.Name != "datafile")
                    {
                        throw new InvalidDataException(Localizer.Get("dat.error.notDatFile"));
                    }

                    reader.Read();

                    // The file is read one game at a time, so a very large DAT never has to fit in memory.
                    while (!reader.EOF)
                    {
                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "game")
                        {
                            // ReadFrom consumes the whole element and leaves the reader after it.
                            DatEntry entry = ParseGame((XElement)XNode.ReadFrom(reader));

                            if (entry != null)
                            {
                                entries.Add(entry);
                            }
                        }
                        else
                        {
                            reader.Read();
                        }
                    }
                }
            }
            catch (XmlException exception)
            {
                throw new InvalidDataException(Localizer.Get("dat.error.notValid"), exception);
            }

            if (entries.Count == 0)
            {
                throw new InvalidDataException(Localizer.Get("dat.error.noGames"));
            }

            return entries;
        }

        private static DatEntry ParseGame(XElement game)
        {
            string name = (string)game.Attribute("name");

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (XElement rom in game.Elements("rom"))
            {
                string crc = (string)rom.Attribute("crc");

                // Entries without a checksum (missing dumps) cannot be matched, so they are skipped.
                if (crc != null && Crc32Pattern.IsMatch(crc))
                {
                    string category = (string)game.Element("category");
                    string datId = (string)game.Attribute("id");
                    string parentDatId = (string)game.Attribute("cloneofid");

                    return new DatEntry(name.Trim(), category?.Trim(), crc.ToUpperInvariant(), datId, parentDatId);
                }
            }

            return null;
        }
    }
}