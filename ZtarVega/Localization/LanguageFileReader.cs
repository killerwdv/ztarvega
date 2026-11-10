using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZtarVega.Localization
{
    /// <summary>
    /// Reads and writes the text format of the language files: one "key = value" per line. Empty lines and
    /// lines that start with "#" or ";" are comments. In a value, "\n" is a line break and "\\" is a backslash.
    /// </summary>
    internal static class LanguageFileReader
    {
        /// <summary>Reads every text of a language file. Lines that are not valid are ignored.</summary>
        public static Dictionary<string, string> Read(string path)
        {
            var texts = new Dictionary<string, string>(StringComparer.Ordinal);

            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();

                    if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == ';')
                    {
                        continue;
                    }

                    int separator = trimmed.IndexOf('=');

                    if (separator <= 0)
                    {
                        continue;
                    }

                    string key = trimmed.Substring(0, separator).Trim();

                    if (key.Length > 0)
                    {
                        texts[key] = Unescape(trimmed.Substring(separator + 1).Trim());
                    }
                }
            }

            return texts;
        }

        /// <summary>Converts a text to the form it has inside a language file (line breaks and backslashes are escaped).</summary>
        public static string Escape(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string Unescape(string text)
        {
            if (text.IndexOf('\\') < 0)
            {
                return text;
            }

            var builder = new StringBuilder(text.Length);

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                if (character == '\\' && index + 1 < text.Length)
                {
                    char next = text[index + 1];

                    if (next == 'n')
                    {
                        builder.Append('\n');
                        index++;
                        continue;
                    }

                    if (next == 't')
                    {
                        builder.Append('\t');
                        index++;
                        continue;
                    }

                    if (next == '\\')
                    {
                        builder.Append('\\');
                        index++;
                        continue;
                    }
                }

                builder.Append(character);
            }

            return builder.ToString();
        }
    }
}