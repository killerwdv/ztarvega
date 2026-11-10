using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZtarVega.Data;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Adds ROM files that match no game to the catalog as new games. When a file looks like a variant of a
    /// game already in the catalog (for example a translation of an official game), it is linked to it.
    /// </summary>
    internal sealed class RomImporter
    {
        private readonly IGameRepository _repository;

        public RomImporter(IGameRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>Creates one game per ROM and saves them all in a single transaction.</summary>
        public RomImportResult Import(IEnumerable<UnmatchedRom> roms)
        {
            if (roms == null)
            {
                throw new ArgumentNullException(nameof(roms));
            }

            IReadOnlyList<Game> existingGames = _repository.GetAll();
            var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedChecksums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Game existingGame in existingGames)
            {
                if (!string.IsNullOrEmpty(existingGame.RomFilename))
                {
                    usedFileNames.Add(existingGame.RomFilename);
                }

                if (!string.IsNullOrEmpty(existingGame.Crc32))
                {
                    usedChecksums.Add(existingGame.Crc32);
                }
            }

            Dictionary<string, List<Game>> baseIndex = BuildBaseIndex(existingGames);
            var newGames = new List<Game>();
            int skippedCount = 0;
            int linkedCount = 0;

            foreach (UnmatchedRom rom in roms)
            {
                // A ZIP can hold several ROMs, so its file name can legitimately repeat; the checksum tells them apart.
                bool isInsideArchive = rom.EntryName != null;

                // Two files with the same content, or a file name already used by another game,
                // would produce a confusing duplicate entry.
                if (usedChecksums.Contains(rom.Crc32) || (!isInsideArchive && usedFileNames.Contains(rom.FileName)))
                {
                    skippedCount++;
                    continue;
                }

                usedChecksums.Add(rom.Crc32);

                if (!isInsideArchive)
                {
                    usedFileNames.Add(rom.FileName);
                }

                // The game is named after the ROM itself, which inside an archive is not the archive's name.
                string name = rom.NameForTitle;
                GameRegion region = RomTitleParser.DetectRegion(name);
                Game baseGame = FindBaseGame(baseIndex, name, region);

                if (baseGame != null)
                {
                    linkedCount++;
                }

                newGames.Add(new Game
                {
                    Title = RomTitleParser.ExtractTitle(name),
                    Region = region,
                    RomType = RomTitleParser.DetectRomType(name),
                    RomFilename = rom.FileName,
                    Crc32 = rom.Crc32,
                    IsAvailable = true,
                    BaseGameId = baseGame == null ? (int?)null : baseGame.Id
                });
            }

            if (newGames.Count > 0)
            {
                _repository.AddRange(newGames);
            }

            return new RomImportResult(newGames.Count, skippedCount, linkedCount);
        }

        /// <summary>Groups the games that can be a base by their normalized title.</summary>
        private static Dictionary<string, List<Game>> BuildBaseIndex(IEnumerable<Game> games)
        {
            var index = new Dictionary<string, List<Game>>();

            foreach (Game game in games)
            {
                // Translations and hacks are not candidates: the base has to be an official release. Regional
                // versions from the DAT file are fine, because they inherit from their main version and the chain keeps working.
                if (game.RomType == RomType.Translation || game.RomType == RomType.Hack)
                {
                    continue;
                }

                string key = NormalizeTitle(game.Title);

                if (key.Length == 0)
                {
                    continue;
                }

                List<Game> sameTitle;

                if (!index.TryGetValue(key, out sameTitle))
                {
                    sameTitle = new List<Game>();
                    index.Add(key, sameTitle);
                }

                sameTitle.Add(game);
            }

            return index;
        }

        /// <summary>
        /// Returns the base game only when there is no doubt: a single game of the same region, or a single game
        /// with that title. Otherwise the user chooses it by hand.
        /// </summary>
        private static Game FindBaseGame(Dictionary<string, List<Game>> index, string fileName, GameRegion region)
        {
            List<Game> candidates;

            if (!index.TryGetValue(NormalizeTitle(fileName), out candidates))
            {
                return null;
            }

            List<Game> sameRegion = candidates.Where(candidate => candidate.Region == region).ToList();

            if (sameRegion.Count == 1)
            {
                return sameRegion[0];
            }

            return candidates.Count == 1 ? candidates[0] : null;
        }

        /// <summary>
        /// Reduces a name to its letters and digits in lower case, without tags, extension or the position of the
        /// article, so "Flintstones, The - X (USA)" and "The Flintstones - X (U).nes" give the same result.
        /// </summary>
        private static string NormalizeTitle(string name)
        {
            string natural = TitleFormatter.ToDisplayTitle(RomTitleParser.ExtractTitle(name));
            var builder = new StringBuilder(natural.Length);

            foreach (char character in natural)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }
    }
}