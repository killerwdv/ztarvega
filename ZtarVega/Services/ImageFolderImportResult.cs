namespace ZtarVega.Services
{
    /// <summary>The outcome of importing the images of a folder.</summary>
    internal sealed class ImageFolderImportResult
    {
        public ImageFolderImportResult(
            int filesFound,
            int importedCount,
            int alreadyHadImageCount,
            int duplicateCount,
            int unmatchedCount,
            int ambiguousCount,
            int skippedModifiedCount,
            int failedCount,
            bool wasCancelled,
            string reportPath)
        {
            FilesFound = filesFound;
            ImportedCount = importedCount;
            AlreadyHadImageCount = alreadyHadImageCount;
            DuplicateCount = duplicateCount;
            UnmatchedCount = unmatchedCount;
            AmbiguousCount = ambiguousCount;
            SkippedModifiedCount = skippedModifiedCount;
            FailedCount = failedCount;
            WasCancelled = wasCancelled;
            ReportPath = reportPath;
        }

        public int FilesFound { get; }

        /// <summary>Images saved.</summary>
        public int ImportedCount { get; }

        /// <summary>Games skipped because they already had an image of that type (fill-only mode).</summary>
        public int AlreadyHadImageCount { get; }

        /// <summary>Files skipped because another file already supplied the image of the same game.</summary>
        public int DuplicateCount { get; }

        /// <summary>Files that match no game.</summary>
        public int UnmatchedCount { get; }

        /// <summary>Files that could belong to several different games.</summary>
        public int AmbiguousCount { get; }

        /// <summary>Files of hacks and translations, which are not used.</summary>
        public int SkippedModifiedCount { get; }

        /// <summary>Files that could not be read or saved.</summary>
        public int FailedCount { get; }

        public bool WasCancelled { get; }

        /// <summary>The path of the detailed report, or null when it could not be written.</summary>
        public string ReportPath { get; }
    }
}