using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>The result of looking for the game an image file belongs to.</summary>
    internal sealed class ImageMatch
    {
        private ImageMatch(ImageMatchKind kind, Game game, string detail)
        {
            Kind = kind;
            Game = game;
            Detail = detail;
        }

        public ImageMatchKind Kind { get; }

        /// <summary>The matched game. Null when there is no match or it is ambiguous.</summary>
        public Game Game { get; }

        /// <summary>For ambiguous matches, the titles of the games that could own the image.</summary>
        public string Detail { get; }

        public static ImageMatch NoMatch()
        {
            return new ImageMatch(ImageMatchKind.None, null, null);
        }

        public static ImageMatch Exact(Game game)
        {
            return new ImageMatch(ImageMatchKind.Exact, game, null);
        }

        public static ImageMatch ByTitle(Game game)
        {
            return new ImageMatch(ImageMatchKind.Title, game, null);
        }

        public static ImageMatch Ambiguous(string candidates)
        {
            return new ImageMatch(ImageMatchKind.Ambiguous, null, candidates);
        }
    }
}