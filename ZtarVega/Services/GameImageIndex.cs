using System;
using System.Collections.Generic;
using System.Linq;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>Finds the game an image file belongs to by comparing names.</summary>
    internal sealed class GameImageIndex
    {
        private const int MaxChainLength = 16;

        private readonly Dictionary<int, Game> _gamesById;
        private readonly Dictionary<string, List<Game>> _gamesByFullKey = new Dictionary<string, List<Game>>();
        private readonly Dictionary<string, List<Game>> _gamesByTitleKey = new Dictionary<string, List<Game>>();
        private readonly Dictionary<string, List<Game>> _modifiedByTitleKey = new Dictionary<string, List<Game>>();

        public GameImageIndex(IEnumerable<Game> games)
        {
            if (games == null)
            {
                throw new ArgumentNullException(nameof(games));
            }

            List<Game> allGames = games.ToList();
            _gamesById = allGames.ToDictionary(game => game.Id);

            foreach (Game game in allGames)
            {
                Add(_gamesByFullKey, NameKey.Full(game.Title), game);
                Add(_gamesByTitleKey, NameKey.Title(game.Title), game);

                if (game.RomType == RomType.Translation || game.RomType == RomType.Hack)
                {
                    Add(_modifiedByTitleKey, NameKey.SpacedTitle(game.Title), game);
                }
            }
        }

        /// <summary>
        /// Looks for the owner of an image. <paramref name="fileStem"/> is the file name without extension.
        /// A name that matches one game exactly wins. Otherwise the image goes to the main version of the game
        /// with that title (the other versions inherit it). When several different games share the title, the
        /// region in the file name decides; if it does not, the image is reported as ambiguous.
        /// </summary>
        public ImageMatch Find(string fileStem)
        {
            string fullKey = NameKey.Full(fileStem);
            List<Game> exactMatches;

            if (fullKey.Length > 0 && _gamesByFullKey.TryGetValue(fullKey, out exactMatches))
            {
                if (exactMatches.Count == 1)
                {
                    return ImageMatch.Exact(exactMatches[0]);
                }

                // Different games can share a simplified name ("Final Fantasy I, II" and "Final Fantasy III"):
                // the spaced name keeps the difference.
                string spacedKey = NameKey.SpacedFull(fileStem);
                List<Game> precise = exactMatches.Where(game => NameKey.SpacedFull(game.Title) == spacedKey).ToList();

                if (precise.Count == 1)
                {
                    return ImageMatch.Exact(precise[0]);
                }
            }

            string titleKey = NameKey.Title(fileStem);
            List<Game> sameTitle;

            if (titleKey.Length == 0 || !_gamesByTitleKey.TryGetValue(titleKey, out sameTitle))
            {
                return ImageMatch.NoMatch();
            }

            List<Game> roots = sameTitle.Select(Root).GroupBy(root => root.Id).Select(group => group.First()).ToList();

            if (roots.Count == 1)
            {
                return ImageMatch.ByTitle(roots[0]);
            }

            GameRegion region;

            if (RomTitleParser.TryDetectRegion(fileStem, out region))
            {
                List<Game> sameRegion = roots.Where(root => root.Region == region || root.Region == GameRegion.World).ToList();

                if (sameRegion.Count == 1)
                {
                    return ImageMatch.ByTitle(sameRegion[0]);
                }
            }

            return ImageMatch.Ambiguous(string.Join(" | ", roots.Select(root => root.Title)));
        }

        /// <summary>
        /// Looks for the owner of the image of a hack or a translation: the game of that kind with the same title.
        /// Such an image never goes to the original game. When the catalog has none, or several, nothing is chosen.
        /// </summary>
        public ImageMatch FindModified(string fileStem)
        {
            List<Game> candidates;

            if (!_modifiedByTitleKey.TryGetValue(NameKey.SpacedTitle(fileStem), out candidates))
            {
                return ImageMatch.NoMatch();
            }

            if (candidates.Count == 1)
            {
                return ImageMatch.ByTitle(candidates[0]);
            }

            return ImageMatch.Ambiguous(string.Join(" | ", candidates.Select(candidate => candidate.Title)));
        }

        /// <summary>The main version of a game: the end of its chain of base games.</summary>
        private Game Root(Game game)
        {
            Game current = game;

            for (int depth = 0; depth < MaxChainLength && current.BaseGameId.HasValue; depth++)
            {
                Game parent;

                if (!_gamesById.TryGetValue(current.BaseGameId.Value, out parent))
                {
                    break;
                }

                current = parent;
            }

            return current;
        }

        private static void Add(Dictionary<string, List<Game>> index, string key, Game game)
        {
            if (key.Length == 0)
            {
                return;
            }

            List<Game> games;

            if (!index.TryGetValue(key, out games))
            {
                games = new List<Game>();
                index.Add(key, games);
            }

            games.Add(game);
        }
    }
}