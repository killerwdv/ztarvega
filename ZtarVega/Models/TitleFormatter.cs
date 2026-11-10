using System.Text.RegularExpressions;

namespace ZtarVega.Models
{
    /// <summary>
    /// Converts titles written with a trailing article, as ROM preservation databases do so that titles
    /// sort by their first meaningful word ("Flintstones, The"), into their natural form ("The Flintstones").
    /// </summary>
    internal static class TitleFormatter
    {
        // The text after the article can continue with ":", " - " or a "(" group, or end there.
        private static readonly Regex TrailingArticlePattern = new Regex(
            @"^(?<main>.+?), (?<article>The|A|An)(?<rest>:.*| - .*| \(.*|)$",
            RegexOptions.CultureInvariant);

        /// <summary>Returns the natural form of a title, or the title itself when it has no trailing article.</summary>
        public static string ToDisplayTitle(string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                return title;
            }

            Match match = TrailingArticlePattern.Match(title);

            if (!match.Success)
            {
                return title;
            }

            return match.Groups["article"].Value + " " + match.Groups["main"].Value + match.Groups["rest"].Value;
        }
    }
}