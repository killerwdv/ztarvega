namespace ZtarVega.Models
{
    /// <summary>Metadata found for a game. A null value means "nothing to add" for that field.</summary>
    public sealed class GameMetadataUpdate
    {
        public GameMetadataUpdate(int gameId, int? year, string developer, string tags)
        {
            GameId = gameId;
            Year = year;
            Developer = developer;
            Tags = tags;
        }

        public int GameId { get; }

        public int? Year { get; }

        public string Developer { get; }

        public string Tags { get; }
    }
}