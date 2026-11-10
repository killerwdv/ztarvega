using System;
using System.Collections.Generic;
using System.Linq;
using ZtarVega.Data;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Completes the year, developer and genre of the catalog games using metadata files.</summary>
    internal sealed class MetadataImporter
    {
        private const int MaxBaseChainLength = 16;

        private readonly IGameRepository _repository;

        public MetadataImporter(IGameRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// Reads the files and applies what they say about each game. Select all the files at once: the
        /// developer is used to check that games sharing a title really are the same game.
        /// When <paramref name="replaceExisting"/> is false, only empty fields are filled and the user's own
        /// values are kept; when it is true, the values found in the files overwrite the existing ones.
        /// Games that are still missing data afterwards take it from their base game.
        /// </summary>
        public MetadataImportResult Import(IReadOnlyList<string> filePaths, bool replaceExisting)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                throw new ArgumentException("At least one file is required.", nameof(filePaths));
            }

            var index = new MetadataIndex();

            foreach (string filePath in filePaths)
            {
                index.Add(ClrMameProReader.Read(filePath));
            }

            IReadOnlyList<Game> games = _repository.GetAll();
            Dictionary<int, Game> gamesById = games.ToDictionary(game => game.Id);

            // What the files say about each game, found by its name.
            var foundByGameId = new Dictionary<int, GameMetadata>();

            foreach (Game game in games)
            {
                GameMetadata metadata = index.Find(game.Title);

                if (metadata != null)
                {
                    foundByGameId.Add(game.Id, metadata);
                }
            }

            var updates = new List<GameMetadataUpdate>();
            int inheritedGames = 0;

            foreach (Game game in games)
            {
                FieldValues target = Resolve(game, gamesById, foundByGameId, replaceExisting, 0);

                // Only what actually changes is written; null means "leave this field as it is".
                int? year = target.Year != game.Year ? target.Year : null;
                string developer = string.Equals(target.Developer, Normalize(game.Developer), StringComparison.Ordinal) ? null : target.Developer;
                string tags = string.Equals(target.Tags, Normalize(game.Tags), StringComparison.Ordinal) ? null : target.Tags;

                if (!year.HasValue && developer == null && tags == null)
                {
                    continue;
                }

                updates.Add(new GameMetadataUpdate(game.Id, year, developer, tags));

                if (target.UsedBase)
                {
                    inheritedGames++;
                }
            }

            if (updates.Count > 0)
            {
                _repository.UpdateMetadata(updates, replaceExisting);
            }

            return new MetadataImportResult(index.EntryCount, games.Count, foundByGameId.Count, updates.Count, inheritedGames);
        }

        /// <summary>
        /// Works out the values a game will have after the import: its own values updated with what the files
        /// say, and then, for the fields still empty, the values of its base game.
        /// </summary>
        private static FieldValues Resolve(
            Game game,
            IReadOnlyDictionary<int, Game> gamesById,
            IReadOnlyDictionary<int, GameMetadata> foundByGameId,
            bool replaceExisting,
            int depth)
        {
            GameMetadata metadata;
            foundByGameId.TryGetValue(game.Id, out metadata);

            int? year = game.Year;
            string developer = Normalize(game.Developer);
            string tags = Normalize(game.Tags);

            if (metadata != null)
            {
                if (metadata.Year.HasValue && (replaceExisting || !year.HasValue))
                {
                    year = metadata.Year;
                }

                if (metadata.Developer != null && (replaceExisting || developer == null))
                {
                    developer = metadata.Developer;
                }

                if (metadata.Genre != null && (replaceExisting || tags == null))
                {
                    tags = metadata.Genre;
                }
            }

            bool isComplete = year.HasValue && developer != null && tags != null;

            if (isComplete || depth >= MaxBaseChainLength || !game.BaseGameId.HasValue)
            {
                return new FieldValues(year, developer, tags, false);
            }

            Game baseGame;

            if (!gamesById.TryGetValue(game.BaseGameId.Value, out baseGame))
            {
                return new FieldValues(year, developer, tags, false);
            }

            // A variant (translation, hack) shares its data with its original, but only to fill what is still empty.
            FieldValues inherited = Resolve(baseGame, gamesById, foundByGameId, replaceExisting, depth + 1);

            bool usedBase = (!year.HasValue && inherited.Year.HasValue) ||
                            (developer == null && inherited.Developer != null) ||
                            (tags == null && inherited.Tags != null);

            return new FieldValues(
                year ?? inherited.Year,
                developer ?? inherited.Developer,
                tags ?? inherited.Tags,
                usedBase);
        }

        private static string Normalize(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        /// <summary>The values of the three fields of a game after the import.</summary>
        private sealed class FieldValues
        {
            public FieldValues(int? year, string developer, string tags, bool usedBase)
            {
                Year = year;
                Developer = developer;
                Tags = tags;
                UsedBase = usedBase;
            }

            public int? Year { get; }

            public string Developer { get; }

            public string Tags { get; }

            /// <summary>True when at least one value comes from the base game.</summary>
            public bool UsedBase { get; }
        }
    }
}