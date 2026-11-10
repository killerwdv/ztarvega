using System;
using System.Collections.Generic;
using System.Linq;
using ZtarVega.Data;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Holds the game catalog in memory and provides fast filtering and searching.</summary>
    internal sealed class GameCatalog
    {
        private const int RecentGamesLimit = 50;

        private readonly IGameRepository _repository;
        private IReadOnlyList<Game> _allGames = new List<Game>();
        private const int MaxBaseChainLength = 16;

        private Dictionary<int, Game> _gamesById = new Dictionary<int, Game>();

        public GameCatalog(IGameRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>The number of games in the catalog.</summary>
        public int GameCount
        {
            get { return _allGames.Count; }
        }

        /// <summary>The number of games whose ROM the user has.</summary>
        public int AvailableCount
        {
            get { return _allGames.Count(game => game.IsAvailable); }
        }

        /// <summary>True when at least one game has a developer, which means metadata was imported or entered.</summary>
        public bool HasMetadata
        {
            get { return _allGames.Any(game => !string.IsNullOrWhiteSpace(game.Developer)); }
        }

        /// <summary>Reloads every game from the database and links each game to its base game.</summary>
        public void Reload()
        {
            IReadOnlyList<Game> games = _repository.GetAll();
            _gamesById = games.ToDictionary(game => game.Id);

            foreach (Game game in games)
            {
                Game baseGame;
                game.BaseGame = game.BaseGameId.HasValue && _gamesById.TryGetValue(game.BaseGameId.Value, out baseGame)
                    ? baseGame
                    : null;
            }

            _allGames = games;
        }

        /// <summary>
        /// Returns the games matching the filter mode and the (optional) title search text.
        /// Most modes are ordered by title; "recently played" is ordered by last played, newest first.
        /// </summary>
        public IReadOnlyList<Game> Search(string searchText, GameFilterMode filterMode)
        {
            return Search(new GameQuery { SearchText = searchText, Mode = filterMode });
        }

        /// <summary>Returns the games that match every condition of the query.</summary>
        public IReadOnlyList<Game> Search(GameQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            IEnumerable<Game> games = _allGames;

            switch (query.Mode)
            {
                case GameFilterMode.AvailableOnly:
                    games = games.Where(game => game.IsAvailable);
                    break;
                case GameFilterMode.FavoritesOnly:
                    games = games.Where(game => game.IsFavorite);
                    break;
                case GameFilterMode.RecentlyPlayed:
                    games = games.Where(game => game.LastPlayed.HasValue);
                    break;
            }

            if (query.Region.HasValue)
            {
                GameRegion region = query.Region.Value;
                games = games.Where(game => game.Region == region);
            }

            if (!string.IsNullOrEmpty(query.Genre))
            {
                string genre = query.Genre;
                games = games.Where(game => HasTag(game, genre));
            }

            if (query.HideAlternateVersions)
            {
                games = games.Where(game => !IsHiddenAlternate(game));
            }

            if (!string.IsNullOrWhiteSpace(query.SearchText))
            {
                string term = query.SearchText.Trim();

                // Both forms are searched, so "The Flintstones" finds the entry stored as "Flintstones, The".
                games = games.Where(game =>
                    game.Title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    game.DisplayTitle.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (query.Mode == GameFilterMode.RecentlyPlayed)
            {
                games = games.OrderByDescending(game => game.LastPlayed).Take(RecentGamesLimit);
            }

            return games.ToList();
        }

        /// <summary>Returns every genre found in the tags of the games, without repeats, sorted alphabetically.</summary>
        public IReadOnlyList<string> GetGenres()
        {
            var genres = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (Game game in _allGames)
            {
                if (string.IsNullOrWhiteSpace(game.Tags))
                {
                    continue;
                }

                foreach (string part in game.Tags.Split(','))
                {
                    string genre = part.Trim();

                    if (genre.Length > 0 && !genres.ContainsKey(genre))
                    {
                        genres.Add(genre, genre);
                    }
                }
            }

            return genres.Values.OrderBy(genre => genre, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool HasTag(Game game, string tag)
        {
            if (string.IsNullOrWhiteSpace(game.Tags))
            {
                return false;
            }

            return game.Tags.Split(',').Any(part => string.Equals(part.Trim(), tag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// An alternate version is a regional version or revision linked to a main version. It is hidden unless
        /// the user has its ROM. Translations, hacks and homebrew are never considered alternates.
        /// </summary>
        private static bool IsHiddenAlternate(Game game)
        {
            if (!game.BaseGameId.HasValue || game.IsAvailable)
            {
                return false;
            }

            return game.RomType != RomType.Translation && game.RomType != RomType.Hack && game.RomType != RomType.Homebrew;
        }

        /// <summary>Records that a game was launched, both in the database and in the in-memory copy.</summary>
        public void RegisterPlay(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _repository.RegisterPlay(game.Id);

            // Keep the in-memory object consistent with the database.
            game.LastPlayed = DateTime.UtcNow;
            game.PlayCount++;
        }

        /// <summary>Marks or unmarks a game as favorite, both in the database and in the in-memory copy.</summary>
        public void SetFavorite(Game game, bool isFavorite)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _repository.SetFavorite(game.Id, isFavorite);
            game.IsFavorite = isFavorite;
        }

        /// <summary>Adds a new game and reloads the catalog. The game receives its generated id.</summary>
        public void AddGame(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _repository.Add(game);
            Reload();
        }

        /// <summary>Saves the changes of an existing game and reloads the catalog.</summary>
        public void UpdateGame(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _repository.Update(game);
            Reload();
        }

        /// <summary>Removes a game from the catalog. Its ROM file and images are not touched.</summary>
        public void DeleteGame(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            _repository.Delete(game.Id);
            Reload();
        }

        /// <summary>Returns the game with the given id, or null when it does not exist.</summary>
        public Game GetById(int id)
        {
            Game game;
            return _gamesById.TryGetValue(id, out game) ? game : null;
        }

        /// <summary>
        /// Returns the games that can be chosen as the base of the given game: every game except itself and
        /// the games that are based on it (directly or not), which would create a loop. At most
        /// <paramref name="limit"/> games are returned.
        /// </summary>
        public IReadOnlyList<Game> FindBaseCandidates(Game game, string searchText, int limit)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return Search(searchText, GameFilterMode.All)
                .Where(candidate => !IsBasedOn(candidate, game.Id))
                .Take(limit)
                .ToList();
        }

        /// <summary>True when the candidate is the game with the given id or descends from it through its chain of base games.</summary>
        private static bool IsBasedOn(Game candidate, int gameId)
        {
            int depth = 0;

            for (Game current = candidate; current != null && depth < MaxBaseChainLength; current = current.BaseGame, depth++)
            {
                if (current.Id == gameId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}