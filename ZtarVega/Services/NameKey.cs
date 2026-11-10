using System.Text;
using System.Text.RegularExpressions;
using ZtarVega.Models;

namespace ZtarVega.Services
{
    /// <summary>
    /// Builds comparison keys from game names, so names that different databases write slightly differently
    /// ("Flintstones, The - X (USA) (En)" and "The Flintstones: X (USA)") produce the same key.
    /// There are two flavors: the "simplified" keys ignore every punctuation mark (very tolerant, but they can
    /// merge different games such as "Final Fantasy I, II" and "Final Fantasy III"), and the "spaced" keys use
    /// punctuation as a word separator (precise).
    /// </summary>
    internal static class NameKey
    {
        // A "(...)" group (captured) or a "[...]" group (not captured).
        private static readonly Regex TagGroupPattern = new Regex(@"\(([^)]*)\)|\[[^\]]*\]", RegexOptions.Compiled);

        // A list of two-letter language codes such as "En" or "En,Fr,De".
        private static readonly Regex LanguageListPattern = new Regex(@"^[A-Z][a-z](,[A-Z][a-z])*$", RegexOptions.Compiled);

        /// <summary>The simplified key of a complete name: it keeps region and revision, but ignores language tags, the position of the article, case and punctuation.</summary>
        public static string Full(string name)
        {
            return Simplify(Prepare(name));
        }

        /// <summary>The simplified key of the title alone, without any tag: all the versions of a game share it.</summary>
        public static string Title(string name)
        {
            return Simplify(TitleFormatter.ToDisplayTitle(RomTitleParser.ExtractTitle(name)));
        }

        /// <summary>Like <see cref="Full"/>, but punctuation separates words, so different games never share a key.</summary>
        public static string SpacedFull(string name)
        {
            return Spaced(Prepare(name));
        }

        /// <summary>Like <see cref="Title"/>, but punctuation separates words.</summary>
        public static string SpacedTitle(string name)
        {
            return Spaced(TitleFormatter.ToDisplayTitle(RomTitleParser.ExtractTitle(name)));
        }

        /// <summary>Removes the language tags and moves a trailing article to the front.</summary>
        private static string Prepare(string name)
        {
            string withoutLanguages = TagGroupPattern.Replace(
                name,
                match => match.Groups[1].Success && LanguageListPattern.IsMatch(match.Groups[1].Value)
                    ? string.Empty
                    : match.Value);

            return TitleFormatter.ToDisplayTitle(withoutLanguages);
        }

        /// <summary>Keeps only letters and digits, in lower case.</summary>
        private static string Simplify(string text)
        {
            var builder = new StringBuilder(text.Length);

            foreach (char character in text)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Letters and digits in lower case. Apostrophes disappear ("Kirby's" = "Kirbys"); any other character
        /// separates words, and a run of separators counts as a single space.
        /// </summary>
        private static string Spaced(string text)
        {
            var builder = new StringBuilder(text.Length);
            bool pendingSeparator = false;

            foreach (char character in text)
            {
                if (char.IsLetterOrDigit(character))
                {
                    if (pendingSeparator && builder.Length > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(char.ToLowerInvariant(character));
                    pendingSeparator = false;
                }
                else if (character != '\'' && character != '\u2019')
                {
                    pendingSeparator = true;
                }
            }

            return builder.ToString();
        }
    }
}