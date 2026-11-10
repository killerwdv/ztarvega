using System;

namespace ZtarVega.Localization
{
    /// <summary>A language the user can choose.</summary>
    internal sealed class LanguageInfo
    {
        public LanguageInfo(string code, string name, string author)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Author = author;
        }

        /// <summary>The code of the language, which is also the name of its file without the extension (for example "es").</summary>
        public string Code { get; }

        /// <summary>The name of the language written in that language (for example "Español").</summary>
        public string Name { get; }

        /// <summary>The author of the translation, or null.</summary>
        public string Author { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}