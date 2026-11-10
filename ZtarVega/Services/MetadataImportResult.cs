namespace ZtarVega.Services
{
    /// <summary>The outcome of importing metadata files into the catalog.</summary>
    internal sealed class MetadataImportResult
    {
        public MetadataImportResult(int entriesRead, int totalGames, int matchedGames, int updatedGames, int inheritedGames)
        {
            EntriesRead = entriesRead;
            TotalGames = totalGames;
            MatchedGames = matchedGames;
            UpdatedGames = updatedGames;
            InheritedGames = inheritedGames;
        }

        /// <summary>Entries read from the files.</summary>
        public int EntriesRead { get; }

        /// <summary>Games in the catalog.</summary>
        public int TotalGames { get; }

        /// <summary>Games for which the files had some data.</summary>
        public int MatchedGames { get; }

        /// <summary>Games that received at least one new value.</summary>
        public int UpdatedGames { get; }

        /// <summary>Games that received at least one value from their base game.</summary>
        public int InheritedGames { get; }
    }
}