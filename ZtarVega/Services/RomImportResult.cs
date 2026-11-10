namespace ZtarVega.Services
{
    /// <summary>The outcome of importing ROM files into the catalog.</summary>
    internal sealed class RomImportResult
    {
        public RomImportResult(int importedCount, int skippedCount, int linkedCount)
        {
            ImportedCount = importedCount;
            SkippedCount = skippedCount;
            LinkedCount = linkedCount;
        }

        public int ImportedCount { get; }

        /// <summary>Files that were not imported because the catalog already has that checksum or file name.</summary>
        public int SkippedCount { get; }

        /// <summary>Imported games that were linked to a base game automatically.</summary>
        public int LinkedCount { get; }
    }
}