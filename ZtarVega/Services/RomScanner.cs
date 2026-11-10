using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZtarVega.Data;
using ZtarVega.Localization;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Scans a folder for ROM files (loose or inside ZIP archives) and matches them against the game catalog.</summary>
    internal sealed class RomScanner
    {
        private const string RomFileExtension = ".nes";
        private const int ProgressReportInterval = 20;

        private readonly IGameRepository _repository;

        public RomScanner(IGameRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Scans the folder on a background thread. Nothing is saved when the scan is cancelled
        /// (the task then ends as cancelled). Progress is reported every few files.
        /// </summary>
        public Task<ScanResult> ScanAsync(string romsFolder, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => Scan(romsFolder, progress, cancellationToken), cancellationToken);
        }

        private ScanResult Scan(string romsFolder, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(romsFolder))
            {
                throw new ArgumentException("The ROMs folder path cannot be empty.", nameof(romsFolder));
            }

            if (!Directory.Exists(romsFolder))
            {
                throw new DirectoryNotFoundException(Localizer.Format("scan.error.folderMissing", romsFolder));
            }

            // Read from the database (not from the UI catalog) so this thread owns its own objects.
            IReadOnlyList<Game> games = _repository.GetAll();
            var gamesByChecksum = new Dictionary<string, Game>(StringComparer.OrdinalIgnoreCase);
            var gamesByFileName = new Dictionary<string, Game>(StringComparer.OrdinalIgnoreCase);

            foreach (Game game in games)
            {
                if (!string.IsNullOrEmpty(game.Crc32) && !gamesByChecksum.ContainsKey(game.Crc32))
                {
                    gamesByChecksum.Add(game.Crc32, game);
                }

                if (!string.IsNullOrEmpty(game.RomFilename) && !gamesByFileName.ContainsKey(game.RomFilename))
                {
                    gamesByFileName.Add(game.RomFilename, game);
                }
            }

            List<string> romFiles = FindRomFiles(romsFolder);
            var matches = new Dictionary<int, GameRomUpdate>();
            var unmatchedRoms = new List<UnmatchedRom>();
            int duplicateFiles = 0;
            int unreadableFiles = 0;

            for (int index = 0; index < romFiles.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string path = romFiles[index];
                string fileName = Path.GetFileName(path);

                if (index % ProgressReportInterval == 0)
                {
                    progress?.Report(new ScanProgress(index, romFiles.Count, fileName));
                }

                IReadOnlyList<ScannedRom> roms;

                try
                {
                    roms = ReadRoms(path, cancellationToken);
                }
                catch (IOException)
                {
                    unreadableFiles++;
                    continue;
                }
                catch (UnauthorizedAccessException)
                {
                    unreadableFiles++;
                    continue;
                }
                catch (InvalidDataException)
                {
                    // A damaged ZIP archive.
                    unreadableFiles++;
                    continue;
                }

                foreach (ScannedRom rom in roms)
                {
                    // The file name is only a fallback for loose files; inside an archive, the checksum decides.
                    Game matchedGame = FindMatchingGame(
                        rom.Crc32,
                        rom.EntryName == null ? fileName : null,
                        gamesByChecksum,
                        gamesByFileName);

                    if (matchedGame == null)
                    {
                        unmatchedRoms.Add(new UnmatchedRom(fileName, rom.Crc32, rom.EntryName));
                        continue;
                    }

                    GameRomUpdate existingMatch;

                    if (matches.TryGetValue(matchedGame.Id, out existingMatch))
                    {
                        duplicateFiles++;

                        // Between two copies of the same ROM, keep the one with the registered file name.
                        if (!HasRegisteredName(matchedGame, existingMatch.RomFilename) && HasRegisteredName(matchedGame, fileName))
                        {
                            matches[matchedGame.Id] = new GameRomUpdate(matchedGame.Id, fileName, rom.Crc32, true);
                        }

                        continue;
                    }

                    matches.Add(matchedGame.Id, new GameRomUpdate(matchedGame.Id, fileName, rom.Crc32, true));
                }
            }

            return ApplyResults(games, matches, romFiles.Count, unmatchedRoms, duplicateFiles, unreadableFiles, cancellationToken);
        }

        private ScanResult ApplyResults(
            IReadOnlyList<Game> games,
            Dictionary<int, GameRomUpdate> matches,
            int romFilesFound,
            List<UnmatchedRom> unmatchedRoms,
            int duplicateFiles,
            int unreadableFiles,
            CancellationToken cancellationToken)
        {
            var updates = new List<GameRomUpdate>();
            int newlyAvailable = 0;
            int noLongerAvailable = 0;

            foreach (Game game in games)
            {
                GameRomUpdate match;

                if (matches.TryGetValue(game.Id, out match))
                {
                    if (!game.IsAvailable)
                    {
                        newlyAvailable++;
                    }

                    // Only write the games whose data actually changes.
                    bool hasChanged = !game.IsAvailable ||
                                      !string.Equals(game.RomFilename, match.RomFilename, StringComparison.Ordinal) ||
                                      !string.Equals(game.Crc32, match.Crc32, StringComparison.OrdinalIgnoreCase);

                    if (hasChanged)
                    {
                        updates.Add(match);
                    }
                }
                else if (game.IsAvailable)
                {
                    noLongerAvailable++;
                    updates.Add(new GameRomUpdate(game.Id, game.RomFilename, game.Crc32, false));
                }
            }

            // Last chance to cancel: after this point the changes are saved.
            cancellationToken.ThrowIfCancellationRequested();

            if (updates.Count > 0)
            {
                _repository.UpdateRomInfo(updates);
            }

            return new ScanResult(
                romFilesFound: romFilesFound,
                gamesAvailable: matches.Count,
                newlyAvailable: newlyAvailable,
                noLongerAvailable: noLongerAvailable,
                unmatchedRoms: unmatchedRoms,
                duplicateFiles: duplicateFiles,
                unreadableFiles: unreadableFiles);
        }

        /// <summary>Reads the ROMs of a file: one for a loose ROM file, any number for a ZIP archive.</summary>
        private static IReadOnlyList<ScannedRom> ReadRoms(string path, CancellationToken cancellationToken)
        {
            if (ZipRomReader.IsArchive(path))
            {
                return ZipRomReader.ReadRoms(path, cancellationToken);
            }

            return new[] { new ScannedRom(null, Crc32Calculator.ComputeRomChecksum(path, cancellationToken)) };
        }

        private static List<string> FindRomFiles(string romsFolder)
        {
            // On Windows a search pattern such as "*.nes" also matches longer extensions (".nes2"),
            // so every file is checked against the exact extensions instead.
            return Directory.EnumerateFiles(romsFolder, "*", SearchOption.TopDirectoryOnly)
                .Where(IsSupportedFile)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsSupportedFile(string path)
        {
            string extension = Path.GetExtension(path);

            return string.Equals(extension, RomFileExtension, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ZipRomReader.ArchiveExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static Game FindMatchingGame(
            string checksum,
            string fileName,
            Dictionary<string, Game> gamesByChecksum,
            Dictionary<string, Game> gamesByFileName)
        {
            Game game;

            if (gamesByChecksum.TryGetValue(checksum, out game))
            {
                return game;
            }

            // The file name is only trusted for games without a checksum yet. When a checksum is
            // already known and differs, the file is a different version of the ROM.
            if (fileName != null && gamesByFileName.TryGetValue(fileName, out game) && string.IsNullOrEmpty(game.Crc32))
            {
                return game;
            }

            return null;
        }

        private static bool HasRegisteredName(Game game, string fileName)
        {
            return string.Equals(game.RomFilename, fileName, StringComparison.OrdinalIgnoreCase);
        }
    }
}