using System.IO;
using System.Text;

namespace ZtarVega.Localization
{
    /// <summary>Writes the template that translators copy to create a new language.</summary>
    internal static class LanguageTemplateWriter
    {
        private const string TemplateFileName = "_template_en.txt";

        private const string Instructions =
            "# ZtarVega language template (English)\r\n" +
            "#\r\n" +
            "# HOW TO ADD A LANGUAGE\r\n" +
            "# 1. Copy this file in the same folder and name the copy with the language code and the extension .lang\r\n" +
            "#    (for example: fr.lang, de.lang, pt-BR.lang).\r\n" +
            "# 2. Translate the text after the \"=\" of every line. Do not change the keys (the text before the \"=\").\r\n" +
            "# 3. Write the name of the language (in that language) in language.name, and your name in language.author.\r\n" +
            "# 4. Save the file as UTF-8, restart ZtarVega and choose the language in Tools > Settings.\r\n" +
            "#\r\n" +
            "# RULES\r\n" +
            "# - Keep the placeholders such as {0} and {1}: they are replaced by values when the text is shown.\r\n" +
            "# - Use \\n for a line break and \\\\ for a backslash.\r\n" +
            "# - An & in front of a letter makes it the keyboard shortcut of a menu item (&Game = Alt+G).\r\n" +
            "# - A line you leave empty or delete is shown in English, so a partial translation works.\r\n" +
            "# - This file is written again every time ZtarVega starts: translate your copy, not this file.\r\n";

        /// <summary>Writes the template into the languages folder. A failure is ignored: the template is only a convenience.</summary>
        public static void Write(string languagesFolder)
        {
            try
            {
                Directory.CreateDirectory(languagesFolder);

                var builder = new StringBuilder(Instructions);
                string section = null;

                foreach (var entry in EnglishTexts.Entries)
                {
                    int dot = entry.Key.IndexOf('.');
                    string entrySection = dot > 0 ? entry.Key.Substring(0, dot) : entry.Key;

                    if (entrySection != section)
                    {
                        builder.Append("\r\n# ").Append(entrySection).Append("\r\n");
                        section = entrySection;
                    }

                    builder.Append(entry.Key).Append(" = ").Append(LanguageFileReader.Escape(entry.Value)).Append("\r\n");
                }

                File.WriteAllText(Path.Combine(languagesFolder, TemplateFileName), builder.ToString(), new UTF8Encoding(true));
            }
            catch (IOException)
            {
            }
            catch (System.UnauthorizedAccessException)
            {
            }
        }
    }
}