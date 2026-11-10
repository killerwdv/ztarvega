using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>The conditions a list of games has to satisfy. A condition left empty does not filter.</summary>
    internal sealed class GameQuery
    {
        public string SearchText { get; set; }

        public GameFilterMode Mode { get; set; }

        /// <summary>Only the games of this region, or null for all.</summary>
        public GameRegion? Region { get; set; }

        /// <summary>Only the games that have this genre among their tags, or null for all.</summary>
        public string Genre { get; set; }

        /// <summary>Hides regional versions and revisions of a game that is shown, unless the user has their ROM.</summary>
        public bool HideAlternateVersions { get; set; }
    }
}