using System.Collections.Generic;
using ZtarVega.Models;

namespace ZtarVega.Data
{
    /// <summary>Provides persistence operations for <see cref="Game"/> entities.</summary>
    internal interface IGameRepository
    {
        /// <summary>Returns every game, ordered by title.</summary>
        IReadOnlyList<Game> GetAll();

        /// <summary>Returns the game with the given id, or null when it does not exist.</summary>
        Game GetById(int id);

        /// <summary>Inserts a new game and returns its generated id.</summary>
        int Add(Game game);

        /// <summary>Updates an existing game. Throws when the game does not exist.</summary>
        void Update(Game game);

        void Delete(int id);

        void SetFavorite(int id, bool isFavorite);

        /// <summary>Stores the current time as last played and increments the play counter.</summary>
        void RegisterPlay(int id);

        /// <summary>Inserts many games in a single transaction. Much faster than calling <see cref="Add"/> repeatedly.</summary>
        void AddRange(IEnumerable<Game> games);

        /// <summary>Updates the ROM file name, checksum and availability of many games in a single transaction.</summary>
        void UpdateRomInfo(IEnumerable<GameRomUpdate> updates);

        /// <summary>
        /// Updates the year, developer and tags of many games in a single transaction. A null value means
        /// "leave this field as it is". When <paramref name="replaceExisting"/> is false, a field is only
        /// written if it is empty, so values entered by the user are never overwritten.
        /// </summary>
        void UpdateMetadata(IEnumerable<GameMetadataUpdate> updates, bool replaceExisting);

        /// <summary>
        /// Links games to their base games in a single transaction. A game that already has a base game keeps it,
        /// so a link chosen by the user is never overwritten.
        /// </summary>
        void SetBaseGames(IEnumerable<GameLink> links);
    }
}