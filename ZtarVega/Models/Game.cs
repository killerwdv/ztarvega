using System;

namespace ZtarVega.Models
{
    /// <summary>Represents a game entry in the catalog.</summary>
    public class Game
    {
        private string _title;
        private string _displayTitle;
        public int Id { get; set; }

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                _displayTitle = null;
            }
        }

        /// <summary>The title in its natural reading form, e.g. "The Flintstones" for "Flintstones, The". Not stored in the database.</summary>
        public string DisplayTitle
        {
            get { return _displayTitle ?? (_displayTitle = TitleFormatter.ToDisplayTitle(_title)); }
        }

        /// <summary>Release year. Null when unknown.</summary>
        public int? Year { get; set; }

        public RomType RomType { get; set; }

        public GameRegion Region { get; set; }

        public string Language { get; set; }

        /// <summary>Comma-separated genres or labels.</summary>
        public string Tags { get; set; }

        public string Developer { get; set; }

        public int? Players { get; set; }

        /// <summary>Expected ROM file name, used as a fallback match.</summary>
        public string RomFilename { get; set; }

        /// <summary>CRC32 checksum of the ROM, used to match files regardless of their name.</summary>
        public string Crc32 { get; set; }

        /// <summary>True when the ROM file is present on the local disk.</summary>
        public bool IsAvailable { get; set; }

        public bool IsFavorite { get; set; }

        /// <summary>Last time the game was launched. Null when never played.</summary>
        public DateTime? LastPlayed { get; set; }

        public int PlayCount { get; set; }

        /// <summary>
        /// Id of the game this one is based on (for example, the original of a translation or a hack).
        /// Null when it is not based on another game.
        /// </summary>
        public int? BaseGameId { get; set; }

        /// <summary>The base game itself. It is not stored: the catalog fills it in when it loads the games.</summary>
        public Game BaseGame { get; set; }

        /// <summary>Returns a copy, so a dialog can edit it without touching the original until the user confirms.</summary>
        public Game Clone()
        {
            // Every field is a value type or an immutable string, so a shallow copy is a full copy.
            return (Game)MemberwiseClone();
        }
    }
}