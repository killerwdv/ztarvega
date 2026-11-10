using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Derives a display title, region, language and ROM type from a ROM name that follows the
    /// No-Intro naming convention, e.g. "Game Name (USA, Europe) (En,Fr) (Rev 1)".
    /// </summary>
    internal static class RomTitleParser
    {
        // Matches a "(...)" or "[...]" group together with the whitespace before it.
        private static readonly Regex TagGroupPattern = new Regex(@"\s*(\([^)]*\)|\[[^\]]*\])", RegexOptions.Compiled);
        private static readonly Regex ParenthesisGroupPattern = new Regex(@"\(([^)]*)\)", RegexOptions.Compiled);
        private static readonly Regex WhitespacePattern = new Regex(@"\s+", RegexOptions.Compiled);

        // A list of two-letter language codes such as "En" or "En,Fr,De".
        private static readonly Regex LanguageListPattern = new Regex(@"^[A-Z][a-z](,[A-Z][a-z])*$", RegexOptions.Compiled);

        // GoodTools-style tags: "[T+Spa]" or "[T-Eng]" mark a translation, "[h]" or "[h1C]" a hack.
        private static readonly Regex TranslationTagPattern = new Regex(@"\[T[+-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex HackTagPattern = new Regex(@"\[h\d*[A-Za-z]*\]", RegexOptions.Compiled);

        private static readonly string[] KnownExtensions = { ".nes", ".unh", ".zip", ".7z" };

        private static readonly Dictionary<string, GameRegion> RegionByToken = CreateRegionMap();

        /// <summary>Returns the name without its extension and without tags such as "(USA)" or "[!]".</summary>
        public static string ExtractTitle(string fileName)
        {
            string baseName = StripKnownExtension(fileName);
            string title = TagGroupPattern.Replace(baseName, string.Empty).Replace('_', ' ');
            title = WhitespacePattern.Replace(title, " ").Trim();

            if (title.Length > 0)
            {
                return title;
            }

            // The whole name was made of tags (or empty): fall back to something non-empty.
            baseName = baseName.Trim();
            return baseName.Length > 0 ? baseName : fileName;
        }

        /// <summary>
        /// Detects the region from the tags of the name. A single clear region is used;
        /// when there is none or several (for example "(USA, Europe)"), the game is treated as World.
        /// </summary>
        public static GameRegion DetectRegion(string name)
        {
            GameRegion region;
            TryDetectRegion(name, out region);
            return region;
        }

        /// <summary>
        /// Tries to detect a single clear region from the tags of the name. Returns false when the name has
        /// no region or several different ones; the region is then World.
        /// </summary>
        public static bool TryDetectRegion(string name, out GameRegion region)
        {
            var regions = new HashSet<GameRegion>();

            foreach (string token in GetParenthesisTokens(name))
            {
                GameRegion detected;

                if (RegionByToken.TryGetValue(token, out detected))
                {
                    regions.Add(detected);
                }
            }

            if (regions.Count == 1)
            {
                region = regions.First();
                return true;
            }

            region = GameRegion.World;
            return false;
        }

        /// <summary>Returns the language list of the name (for example "En,Fr"), or null when it has none.</summary>
        public static string DetectLanguage(string name)
        {
            foreach (Match group in ParenthesisGroupPattern.Matches(StripKnownExtension(name)))
            {
                string text = group.Groups[1].Value.Trim();

                if (LanguageListPattern.IsMatch(text))
                {
                    return text;
                }
            }

            return null;
        }

        /// <summary>Detects the ROM type from tags such as "(Pirate)", "(Proto)", "(Beta)", "(Aftermarket)" or "(Unl)".</summary>
        public static RomType DetectRomType(string name)
        {
            List<string> tokens = GetParenthesisTokens(name).ToList();

            if (TranslationTagPattern.IsMatch(name))
            {
                return RomType.Translation;
            }

            if (HackTagPattern.IsMatch(name) || HasTag(tokens, "Hack"))
            {
                return RomType.Hack;
            }

            if (HasTag(tokens, "Pirate"))
            {
                return RomType.Bootleg;
            }

            if (HasTag(tokens, "Proto") || HasTag(tokens, "Beta") || HasTag(tokens, "Alpha"))
            {
                return RomType.Prototype;
            }

            if (HasTag(tokens, "Aftermarket"))
            {
                return RomType.Homebrew;
            }

            if (HasTag(tokens, "Unl"))
            {
                return RomType.Unlicensed;
            }

            return RomType.Original;
        }

        private static Dictionary<string, GameRegion> CreateRegionMap()
        {
            var map = new Dictionary<string, GameRegion>(StringComparer.OrdinalIgnoreCase)
            {
                { "USA", GameRegion.Usa },
                { "U", GameRegion.Usa },
                { "Japan", GameRegion.Japan },
                { "J", GameRegion.Japan },
                { "Europe", GameRegion.Europe },
                { "E", GameRegion.Europe },
                { "World", GameRegion.World },
                { "W", GameRegion.World },
                { "US", GameRegion.Usa },
                { "JP", GameRegion.Japan },
                { "EU", GameRegion.Europe }
            };

            string[] otherRegions =
            {
                "Asia", "Australia", "Brazil", "Canada", "China", "Denmark", "Finland", "France",
                "Germany", "Greece", "Hong Kong", "Italy", "Korea", "Netherlands", "Norway",
                "Portugal", "Russia", "Scandinavia", "Spain", "Sweden", "Taiwan", "UK"
            };

            foreach (string regionName in otherRegions)
            {
                map[regionName] = GameRegion.Other;
            }

            return map;
        }

        /// <summary>Removes the extension only when it is one of the known ROM extensions, so names like "Mr. Gimmick" stay intact.</summary>
        private static string StripKnownExtension(string name)
        {
            foreach (string extension in KnownExtensions)
            {
                if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(0, name.Length - extension.Length);
                }
            }

            return name;
        }

        /// <summary>Returns every comma-separated item found inside parentheses, e.g. "USA", "Europe", "Rev 1".</summary>
        private static IEnumerable<string> GetParenthesisTokens(string name)
        {
            foreach (Match group in ParenthesisGroupPattern.Matches(StripKnownExtension(name)))
            {
                foreach (string token in group.Groups[1].Value.Split(','))
                {
                    string trimmed = token.Trim();

                    if (trimmed.Length > 0)
                    {
                        yield return trimmed;
                    }
                }
            }
        }

        /// <summary>True when a token is the tag itself ("Beta") or starts with it followed by a space ("Beta 2").</summary>
        private static bool HasTag(IEnumerable<string> tokens, string tag)
        {
            return tokens.Any(token =>
                token.Equals(tag, StringComparison.OrdinalIgnoreCase) ||
                token.StartsWith(tag + " ", StringComparison.OrdinalIgnoreCase));
        }
    }
}