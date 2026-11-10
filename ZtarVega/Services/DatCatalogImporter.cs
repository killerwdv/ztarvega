using System;
using System.Collections.Generic;
using System.IO;
using ZtarVega.Data;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Creates the catalog from a DAT file. The games are added without a ROM (not available);
    /// the ROM scanner later marks the ones the user owns by matching their checksum.
    /// The regional versions and revisions are linked to the main version of their game.
    /// </summary>
    internal sealed class DatCatalogImporter
    {
        private const string BiosPrefix = "[BIOS]";
        private const string RomFileExtension = ".nes";

        // Categories that are not playable games. Entries without a category are treated as games.
        private static readonly HashSet<string> IgnoredCategories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Applications", "Add-Ons" };

        private readonly IGameRepository _repository;

        public DatCatalogImporter(IGameRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Adds the games of the DAT file that are not in the catalog yet and links the versions of each game.
        /// Games already in the catalog (same checksum) are left untouched, so importing a newer DAT never
        /// overwrites the user's edits, and a link chosen by the user is never replaced.
        /// </summary>
        public DatImportResult Import(string datFilePath)
        {
            IReadOnlyList<DatEntry> entries = DatFileReader.Read(datFilePath);

            var knownChecksums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Game existingGame in _repository.GetAll())
            {
                if (!string.IsNullOrEmpty(existingGame.Crc32))
                {
                    knownChecksums.Add(existingGame.Crc32);
                }
            }

            var newGames = new List<Game>();
            int alreadyInCatalogCount = 0;
            int ignoredCount = 0;

            foreach (DatEntry entry in entries)
            {
                if (!IsPlayableGame(entry))
                {
                    ignoredCount++;
                    continue;
                }

                // Add returns false when the checksum is already known, either from the catalog
                // or from an earlier entry of the same DAT file.
                if (!knownChecksums.Add(entry.Crc32))
                {
                    alreadyInCatalogCount++;
                    continue;
                }

                newGames.Add(CreateGame(entry));
            }

            if (newGames.Count > 0)
            {
                _repository.AddRange(newGames);
            }

            int linkedCount = LinkVersions(entries);

            return new DatImportResult(newGames.Count, alreadyInCatalogCount, ignoredCount, linkedCount);
        }

        /// <summary>
        /// Links every clone (another region or a revision) to the main version of its game, as the DAT file
        /// describes it. It also works for games that were imported earlier, which is why it runs on every import.
        /// </summary>
        private int LinkVersions(IReadOnlyList<DatEntry> entries)
        {
            // The DAT file identifies its entries with its own ids; a clone points to the id of its parent.
            var checksumByDatId = new Dictionary<string, string>();

            foreach (DatEntry entry in entries)
            {
                if (IsPlayableGame(entry) && !string.IsNullOrEmpty(entry.DatId))
                {
                    checksumByDatId[entry.DatId] = entry.Crc32;
                }
            }

            var gameIdByChecksum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var gamesWithBase = new HashSet<int>();

            foreach (Game game in _repository.GetAll())
            {
                if (!string.IsNullOrEmpty(game.Crc32) && !gameIdByChecksum.ContainsKey(game.Crc32))
                {
                    gameIdByChecksum.Add(game.Crc32, game.Id);
                }

                if (game.BaseGameId.HasValue)
                {
                    gamesWithBase.Add(game.Id);
                }
            }

            var links = new List<GameLink>();

            foreach (DatEntry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.ParentDatId) || !IsPlayableGame(entry))
                {
                    continue;
                }

                string parentChecksum;
                int gameId;
                int parentGameId;

                if (checksumByDatId.TryGetValue(entry.ParentDatId, out parentChecksum) &&
                    gameIdByChecksum.TryGetValue(entry.Crc32, out gameId) &&
                    gameIdByChecksum.TryGetValue(parentChecksum, out parentGameId) &&
                    gameId != parentGameId &&
                    !gamesWithBase.Contains(gameId))
                {
                    links.Add(new GameLink(gameId, parentGameId));
                }
            }

            if (links.Count > 0)
            {
                _repository.SetBaseGames(links);
            }

            return links.Count;
        }

        private static bool IsPlayableGame(DatEntry entry)
        {
            if (entry.Name.StartsWith(BiosPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return entry.Category == null || !IgnoredCategories.Contains(entry.Category);
        }

        private static Game CreateGame(DatEntry entry)
        {
            return new Game
            {
                // The full name is kept as the title so that regional versions and revisions can be told apart in the list.
                Title = entry.Name,
                Region = RomTitleParser.DetectRegion(entry.Name),
                RomType = RomTitleParser.DetectRomType(entry.Name),
                Language = RomTitleParser.DetectLanguage(entry.Name),
                RomFilename = BuildExpectedRomFileName(entry.Name),
                Crc32 = entry.Crc32,
                IsAvailable = false
            };
        }

        /// <summary>The file name the ROM would have following the naming convention. The scanner replaces it with the real one.</summary>
        private static string BuildExpectedRomFileName(string name)
        {
            string fileName = name + RomFileExtension;
            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ? null : fileName;
        }
    }
}