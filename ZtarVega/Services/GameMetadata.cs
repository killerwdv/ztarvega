namespace ZtarVega.Services
{
    /// <summary>The metadata the files provide for one game. A null value means the files have nothing for that field.</summary>
    internal sealed class GameMetadata
    {
        public GameMetadata(int? year, string developer, string genre)
        {
            Year = year;
            Developer = developer;
            Genre = genre;
        }

        public int? Year { get; }

        public string Developer { get; }

        public string Genre { get; }

        public bool HasData
        {
            get { return Year.HasValue || Developer != null || Genre != null; }
        }
    }
}