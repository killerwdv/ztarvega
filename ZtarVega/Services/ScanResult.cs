using System;
using System.Collections.Generic;

namespace ZtarVega.Services
{
    /// <summary>The summary of a completed ROM scan.</summary>
    internal sealed class ScanResult
    {
        public ScanResult(
            int romFilesFound,
            int gamesAvailable,
            int newlyAvailable,
            int noLongerAvailable,
            IReadOnlyList<UnmatchedRom> unmatchedRoms,
            int duplicateFiles,
            int unreadableFiles)
        {
            RomFilesFound = romFilesFound;
            GamesAvailable = gamesAvailable;
            NewlyAvailable = newlyAvailable;
            NoLongerAvailable = noLongerAvailable;
            UnmatchedRoms = unmatchedRoms ?? throw new ArgumentNullException(nameof(unmatchedRoms));
            DuplicateFiles = duplicateFiles;
            UnreadableFiles = unreadableFiles;
        }

        public int RomFilesFound { get; }

        /// <summary>Number of games whose ROM was found.</summary>
        public int GamesAvailable { get; }

        public int NewlyAvailable { get; }

        public int NoLongerAvailable { get; }

        /// <summary>Files that do not correspond to any game of the catalog.</summary>
        public IReadOnlyList<UnmatchedRom> UnmatchedRoms { get; }

        /// <summary>Extra copies of a ROM that was already matched.</summary>
        public int DuplicateFiles { get; }

        /// <summary>Files that could not be read.</summary>
        public int UnreadableFiles { get; }
    }
}