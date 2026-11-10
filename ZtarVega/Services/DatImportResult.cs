namespace ZtarVega.Services
{
    /// <summary>The outcome of importing a DAT file into the catalog.</summary>
    internal sealed class DatImportResult
    {
        public DatImportResult(int addedCount, int alreadyInCatalogCount, int ignoredCount, int linkedCount)
        {
            AddedCount = addedCount;
            AlreadyInCatalogCount = alreadyInCatalogCount;
            IgnoredCount = ignoredCount;
            LinkedCount = linkedCount;
        }

        public int AddedCount { get; }

        /// <summary>Entries skipped because the catalog already has a game with the same checksum.</summary>
        public int AlreadyInCatalogCount { get; }

        /// <summary>Entries that are not games, such as BIOS files and add-ons.</summary>
        public int IgnoredCount { get; }

        /// <summary>Games linked to the main version of their game.</summary>
        public int LinkedCount { get; }
    }
}