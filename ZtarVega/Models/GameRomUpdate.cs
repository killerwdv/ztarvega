namespace ZtarVega.Models
{
    /// <summary>The ROM-related values to store for a game after a scan.</summary>
    public sealed class GameRomUpdate
    {
        public GameRomUpdate(int gameId, string romFilename, string crc32, bool isAvailable)
        {
            GameId = gameId;
            RomFilename = romFilename;
            Crc32 = crc32;
            IsAvailable = isAvailable;
        }

        public int GameId { get; }

        public string RomFilename { get; }

        public string Crc32 { get; }

        public bool IsAvailable { get; }
    }
}