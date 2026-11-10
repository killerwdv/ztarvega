using System;
using System.Text;
using System.Text.RegularExpressions;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// A web address with placeholders that the user writes once, for example
    /// "https://example.com/images/{filetitle}.png". Every game gets its own address.
    /// </summary>
    internal static class ImageUrlTemplate
    {
        public const string TitlePlaceholder = "{title}";
        public const string FileTitlePlaceholder = "{filetitle}";
        public const string Crc32Placeholder = "{crc32}";

        // Characters that cannot be part of a file name. Websites that store images by game name replace them with "_".
        private static readonly char[] InvalidFileTitleCharacters = { '&', '*', '/', ':', '`', '<', '>', '?', '\\', '|', '"' };

        private static readonly Regex PlaceholderPattern =
            new Regex(@"\{(title|filetitle|crc32)\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>Checks that a template can be used. Only web addresses are accepted, never local files.</summary>
        public static TemplateProblem Validate(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return TemplateProblem.Empty;
            }

            string trimmed = template.Trim();

            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return TemplateProblem.UnsupportedScheme;
            }

            // Without a placeholder, every game would get the same address.
            return PlaceholderPattern.IsMatch(trimmed) ? TemplateProblem.None : TemplateProblem.NoPlaceholder;
        }

        /// <summary>
        /// Builds the address of a game. Returns null when the template needs the checksum and the game has none.
        /// The values are encoded, so any character of a title is safe inside an address.
        /// </summary>
        public static string Build(string template, Game game)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            bool isChecksumMissing = false;

            // A single pass, so a placeholder written inside a title is never replaced a second time.
            string url = PlaceholderPattern.Replace(template.Trim(), match =>
            {
                switch (match.Groups[1].Value.ToLowerInvariant())
                {
                    case "title":
                        return Uri.EscapeDataString(game.Title);

                    case "filetitle":
                        return Uri.EscapeDataString(MakeFileTitle(game.Title));

                    default:
                        if (string.IsNullOrEmpty(game.Crc32))
                        {
                            isChecksumMissing = true;
                            return string.Empty;
                        }

                        return game.Crc32.ToUpperInvariant();
                }
            });

            return isChecksumMissing ? null : url;
        }

        /// <summary>Replaces the characters that file names cannot have with an underscore.</summary>
        public static string MakeFileTitle(string title)
        {
            var builder = new StringBuilder(title.Length);

            foreach (char character in title)
            {
                builder.Append(Array.IndexOf(InvalidFileTitleCharacters, character) >= 0 ? '_' : character);
            }

            return builder.ToString();
        }
    }
}