using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using ZtarVega.Localization;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Starts the configured emulator with the ROM of a game.</summary>
    internal sealed class EmulatorLauncher
    {
        private const string ExtractedRomExtension = ".nes";

        private readonly string _extractionFolder;

        /// <param name="extractionFolder">Folder where ROMs are taken out of ZIP archives, because not every emulator can open them.</param>
        public EmulatorLauncher(string extractionFolder)
        {
            if (string.IsNullOrWhiteSpace(extractionFolder))
            {
                throw new ArgumentException("The extraction folder path cannot be empty.", nameof(extractionFolder));
            }

            _extractionFolder = extractionFolder;
        }

        public LaunchResult Launch(Game game, AppSettings settings)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (string.IsNullOrWhiteSpace(settings.EmulatorPath) || !File.Exists(settings.EmulatorPath))
            {
                return LaunchResult.Fail(
                    LaunchFailure.EmulatorNotConfigured,
                    Localizer.Get("launch.error.emulatorNotConfigured"));
            }

            if (string.IsNullOrWhiteSpace(settings.RomsFolder) || !Directory.Exists(settings.RomsFolder))
            {
                return LaunchResult.Fail(
                    LaunchFailure.RomsFolderNotConfigured,
                    Localizer.Get("launch.error.romsFolderNotConfigured"));
            }

            string romPath = ResolveRomPath(game, settings);

            if (romPath == null)
            {
                string reason = string.IsNullOrWhiteSpace(game.RomFilename)
                    ? Localizer.Get("launch.error.noRomFilename")
                    : Localizer.Format("launch.error.romFileNotFound", game.RomFilename);

                return LaunchResult.Fail(LaunchFailure.RomNotFound, reason);
            }

            if (ZipRomReader.IsArchive(romPath))
            {
                // Not every emulator can open ZIP files, so the ROM is taken out of the archive first.
                string extractedPath;
                LaunchResult extractionFailure = TryExtractRom(game, romPath, out extractedPath);

                if (extractionFailure != null)
                {
                    return extractionFailure;
                }

                romPath = extractedPath;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = settings.EmulatorPath,
                Arguments = BuildArguments(settings.EmulatorArguments, romPath),
                WorkingDirectory = Path.GetDirectoryName(settings.EmulatorPath),
                UseShellExecute = false
            };

            try
            {
                // The process is not awaited. Disposing the Process object does not close the emulator.
                using (Process.Start(startInfo))
                {
                }

                return LaunchResult.Ok();
            }
            catch (Win32Exception exception)
            {
                return LaunchResult.Fail(LaunchFailure.StartFailed, Localizer.Format("launch.error.startFailed", exception.Message));
            }
            catch (InvalidOperationException exception)
            {
                return LaunchResult.Fail(LaunchFailure.StartFailed, Localizer.Format("launch.error.startFailed", exception.Message));
            }
        }

        /// <summary>Deletes the extracted ROMs that have not been used for a while. Files in use are left for the next time.</summary>
        public void DeleteOldExtractedFiles(TimeSpan maxAge)
        {
            try
            {
                if (!Directory.Exists(_extractionFolder))
                {
                    return;
                }

                DateTime limit = DateTime.UtcNow - maxAge;

                foreach (string path in Directory.EnumerateFiles(_extractionFolder))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(path) < limit)
                        {
                            File.Delete(path);
                        }
                    }
                    catch (IOException)
                    {
                        // An emulator may still be using it.
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
            catch (IOException)
            {
                // Cleaning up is a courtesy: it must never stop the application from starting.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Takes the ROM of the game out of its archive. Returns null on success, or the reason why it failed.</summary>
        private LaunchResult TryExtractRom(Game game, string archivePath, out string extractedPath)
        {
            extractedPath = null;

            try
            {
                Directory.CreateDirectory(_extractionFolder);

                // The file is named after its checksum: the same ROM is only extracted once.
                string fileStem = !string.IsNullOrEmpty(game.Crc32)
                    ? game.Crc32.ToUpperInvariant()
                    : Path.GetFileNameWithoutExtension(archivePath);
                string destination = Path.Combine(_extractionFolder, fileStem + ExtractedRomExtension);

                if (File.Exists(destination))
                {
                    // Used again: its date is refreshed so it is not cleaned up as old.
                    File.SetLastWriteTimeUtc(destination, DateTime.UtcNow);
                    extractedPath = destination;
                    return null;
                }

                if (!ZipRomReader.TryExtract(archivePath, game.Crc32, destination))
                {
                    return LaunchResult.Fail(
                        LaunchFailure.RomNotFound,
                        Localizer.Format("launch.error.romNotInArchive", Path.GetFileName(archivePath)));
                }

                extractedPath = destination;
                return null;
            }
            catch (InvalidDataException exception)
            {
                return LaunchResult.Fail(LaunchFailure.StartFailed, Localizer.Format("launch.error.zipUnreadable", exception.Message));
            }
            catch (IOException exception)
            {
                return LaunchResult.Fail(LaunchFailure.StartFailed, Localizer.Format("launch.error.extractFailed", exception.Message));
            }
            catch (UnauthorizedAccessException exception)
            {
                return LaunchResult.Fail(LaunchFailure.StartFailed, Localizer.Format("launch.error.extractFailed", exception.Message));
            }
        }

        private static string ResolveRomPath(Game game, AppSettings settings)
        {
            if (string.IsNullOrWhiteSpace(game.RomFilename))
            {
                return null;
            }

            try
            {
                // Only plain file names are accepted, so a stored value such as "..\..\tool.exe"
                // can never point outside of the ROMs folder.
                if (!string.Equals(Path.GetFileName(game.RomFilename), game.RomFilename, StringComparison.Ordinal))
                {
                    return null;
                }

                string path = Path.Combine(settings.RomsFolder, game.RomFilename);
                return File.Exists(path) ? path : null;
            }
            catch (ArgumentException)
            {
                // The file name contains characters that are not valid in a path.
                return null;
            }
        }

        private static string BuildArguments(string template, string romPath)
        {
            string effectiveTemplate = string.IsNullOrWhiteSpace(template) ? AppSettings.DefaultEmulatorArguments : template;
            string quotedRomPath = "\"" + romPath + "\"";

            return effectiveTemplate.Replace(AppSettings.RomPlaceholder, quotedRomPath);
        }
    }
}