namespace ZtarVega.Models
{
    /// <summary>A link that makes a game share the images and data of a base game.</summary>
    public sealed class GameLink
    {
        public GameLink(int gameId, int baseGameId)
        {
            GameId = gameId;
            BaseGameId = baseGameId;
        }

        public int GameId { get; }

        public int BaseGameId { get; }
    }
}