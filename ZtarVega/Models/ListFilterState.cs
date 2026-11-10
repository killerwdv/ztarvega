using System;

namespace ZtarVega.Models
{
    /// <summary>
    /// The filters chosen in the game list, which are remembered between sessions.
    /// The search text is not one of them: a forgotten search would make the list look incomplete.
    /// </summary>
    public sealed class ListFilterState
    {
        public ListFilterState(GameFilterMode mode, GameRegion? region, string genre, bool hideAlternateVersions)
        {
            Mode = mode;
            Region = region;
            Genre = string.IsNullOrWhiteSpace(genre) ? null : genre.Trim();
            HideAlternateVersions = hideAlternateVersions;
        }

        /// <summary>No filter at all: every game is shown.</summary>
        public static ListFilterState Default { get; } = new ListFilterState(GameFilterMode.All, null, null, false);

        public GameFilterMode Mode { get; }

        /// <summary>Only the games of this region, or null for all.</summary>
        public GameRegion? Region { get; }

        /// <summary>Only the games with this genre, or null for all.</summary>
        public string Genre { get; }

        public bool HideAlternateVersions { get; }

        /// <summary>Reads the filters from the settings. Any value that is missing or not valid becomes "no filter".</summary>
        public static ListFilterState FromSettings(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            GameFilterMode mode;

            // Enum.TryParse also accepts numbers, so the value has to be one of the defined ones as well.
            if (!Enum.TryParse(settings.FilterMode, true, out mode) || !Enum.IsDefined(typeof(GameFilterMode), mode))
            {
                mode = GameFilterMode.All;
            }

            GameRegion parsedRegion;
            GameRegion? region =
                Enum.TryParse(settings.FilterRegion, true, out parsedRegion) && Enum.IsDefined(typeof(GameRegion), parsedRegion)
                    ? parsedRegion
                    : (GameRegion?)null;

            return new ListFilterState(mode, region, settings.FilterGenre, settings.HideAlternateVersions);
        }

        /// <summary>Stores the filters in the settings.</summary>
        public void WriteTo(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.FilterMode = Mode.ToString();
            settings.FilterRegion = Region?.ToString();
            settings.FilterGenre = Genre;
            settings.HideAlternateVersions = HideAlternateVersions;
        }
    }
}