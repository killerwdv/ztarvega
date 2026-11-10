namespace ZtarVega.Services
{
    /// <summary>The summary of a batch download of images.</summary>
    internal sealed class ImageDownloadResult
    {
        public ImageDownloadResult(
            int gamesInSelection,
            int downloadedCount,
            int alreadyHadImageCount,
            int notFoundCount,
            int notAnImageCount,
            int noChecksumCount,
            int failedCount,
            bool wasCancelled,
            bool wasStoppedByConnectionErrors,
            string firstNotFoundAddress,
            string reportPath)
        {
            GamesInSelection = gamesInSelection;
            DownloadedCount = downloadedCount;
            AlreadyHadImageCount = alreadyHadImageCount;
            NotFoundCount = notFoundCount;
            NotAnImageCount = notAnImageCount;
            NoChecksumCount = noChecksumCount;
            FailedCount = failedCount;
            WasCancelled = wasCancelled;
            WasStoppedByConnectionErrors = wasStoppedByConnectionErrors;
            FirstNotFoundAddress = firstNotFoundAddress;
            ReportPath = reportPath;
        }

        public int GamesInSelection { get; }

        public int DownloadedCount { get; }

        /// <summary>Games skipped because they already had the image (fill-only mode).</summary>
        public int AlreadyHadImageCount { get; }

        /// <summary>Addresses the website answered that do not exist.</summary>
        public int NotFoundCount { get; }

        /// <summary>Answers that were not images.</summary>
        public int NotAnImageCount { get; }

        /// <summary>Games skipped because the address needs a checksum and the game has none.</summary>
        public int NoChecksumCount { get; }

        /// <summary>Requests that failed (connection problems, files that are too large...).</summary>
        public int FailedCount { get; }

        public bool WasCancelled { get; }

        /// <summary>True when the download was stopped after several connection failures in a row.</summary>
        public bool WasStoppedByConnectionErrors { get; }

        /// <summary>An example of an address that was not found, to help the user fix the template. Null when there was none.</summary>
        public string FirstNotFoundAddress { get; }

        /// <summary>The path of the detailed report, or null when it could not be written.</summary>
        public string ReportPath { get; }
    }
}