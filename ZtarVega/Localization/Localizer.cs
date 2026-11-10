using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ZtarVega.Models;

namespace ZtarVega.Localization
{
    /// <summary>
    /// Provides the texts of the interface in the language chosen by the user. When a text is missing from the
    /// chosen language, the built-in English text is used, so a partial translation always works.
    /// </summary>
    internal static class Localizer
    {
        public const string EnglishCode = "en";
        public const string FileExtension = ".lang";

        private const string LanguageNameKey = "language.name";
        private const string LanguageAuthorKey = "language.author";

        // A language code is also a file name, so only harmless characters are accepted.
        private static readonly Regex CodePattern = new Regex("^[A-Za-z0-9_-]{1,20}$", RegexOptions.None);

        private static Dictionary<string, string> _selectedTexts = new Dictionary<string, string>(StringComparer.Ordinal);
        private static string _languagesFolder = string.Empty;

        /// <summary>The code of the language in use ("en" when the chosen language could not be loaded).</summary>
        public static string CurrentCode { get; private set; } = EnglishCode;

        /// <summary>
        /// Loads the language. An empty code means "automatic": the language of Windows is used when there
        /// is a file for it. Any problem leaves the interface in English.
        /// </summary>
        public static void Initialize(string languagesFolder, string languageCode)
        {
            _languagesFolder = languagesFolder ?? string.Empty;
            _selectedTexts = new Dictionary<string, string>(StringComparer.Ordinal);
            CurrentCode = EnglishCode;

            string code = string.IsNullOrWhiteSpace(languageCode) ? DetectSystemLanguage() : languageCode.Trim();

            if (!IsValidCode(code) || string.Equals(code, EnglishCode, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                _selectedTexts = LanguageFileReader.Read(Path.Combine(_languagesFolder, code + FileExtension));
                CurrentCode = code;
            }
            catch (IOException)
            {
                // A language that cannot be read leaves the application in English.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>Returns the text of a key: the chosen language, else English, else the key itself so a missing text is easy to spot.</summary>
        public static string Get(string key)
        {
            string text;

            if (_selectedTexts.TryGetValue(key, out text) && text.Length > 0)
            {
                return text;
            }

            return EnglishTexts.Find(key) ?? key;
        }

        /// <summary>Returns a text with its placeholders ({0}, {1}...) replaced. A damaged translation never causes an error: the English text is used.</summary>
        public static string Format(string key, params object[] arguments)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
            }
            catch (FormatException)
            {
                string english = EnglishTexts.Find(key);
                return english == null ? key : string.Format(CultureInfo.CurrentCulture, english, arguments);
            }
        }

        public static string RegionName(GameRegion region)
        {
            return Get("region." + region.ToString().ToLowerInvariant());
        }

        public static string RomTypeName(RomType romType)
        {
            return Get("romtype." + romType.ToString().ToLowerInvariant());
        }

        /// <summary>Returns English followed by every valid language file found in the languages folder.</summary>
        public static IReadOnlyList<LanguageInfo> DiscoverLanguages()
        {
            var languages = new List<LanguageInfo>();

            try
            {
                if (Directory.Exists(_languagesFolder))
                {
                    foreach (string path in Directory.EnumerateFiles(_languagesFolder, "*" + FileExtension))
                    {
                        LanguageInfo language = TryReadLanguageInfo(path);

                        if (language != null)
                        {
                            languages.Add(language);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // A folder that cannot be read just offers no extra languages.
            }
            catch (UnauthorizedAccessException)
            {
            }

            languages.Sort((first, second) => string.Compare(first.Name, second.Name, StringComparison.CurrentCultureIgnoreCase));
            languages.Insert(0, new LanguageInfo(EnglishCode, EnglishTexts.Find(LanguageNameKey), null));

            return languages;
        }

        private static LanguageInfo TryReadLanguageInfo(string path)
        {
            string code = Path.GetFileNameWithoutExtension(path);

            if (!IsValidCode(code) || string.Equals(code, EnglishCode, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                Dictionary<string, string> texts = LanguageFileReader.Read(path);
                string name;
                string author;
                texts.TryGetValue(LanguageNameKey, out name);
                texts.TryGetValue(LanguageAuthorKey, out author);

                return new LanguageInfo(code, string.IsNullOrWhiteSpace(name) ? code : name.Trim(), string.IsNullOrWhiteSpace(author) ? null : author.Trim());
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        private static string DetectSystemLanguage()
        {
            CultureInfo culture = CultureInfo.CurrentUICulture;

            // First the exact variant ("es-AR"), then the language alone ("es").
            foreach (string code in new[] { culture.Name, culture.TwoLetterISOLanguageName }.Distinct())
            {
                if (IsValidCode(code) && File.Exists(Path.Combine(_languagesFolder, code + FileExtension)))
                {
                    return code;
                }
            }

            return EnglishCode;
        }

        private static bool IsValidCode(string code)
        {
            return !string.IsNullOrEmpty(code) && CodePattern.IsMatch(code);
        }
    }
}