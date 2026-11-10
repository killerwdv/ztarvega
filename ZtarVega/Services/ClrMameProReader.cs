using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ZtarVega.Localization;

namespace ZtarVega.Services
{
    /// <summary>
    /// Reads the "clrmamepro" text format used by the metadata files of the libretro database, for example:
    /// <c>game ( comment "Name" developer "Irem" rom ( crc 44AA3EEB ) )</c>.
    /// </summary>
    internal static class ClrMameProReader
    {
        private static readonly Regex Crc32Pattern = new Regex("^[0-9A-Fa-f]{8}$", RegexOptions.Compiled);

        /// <summary>Reads every game of the file. Throws <see cref="InvalidDataException"/> when the file has another format.</summary>
        public static IReadOnlyList<MetadataEntry> Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("The file path cannot be empty.", nameof(path));
            }

            var entries = new List<MetadataEntry>();

            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                var tokenizer = new Tokenizer(reader);
                Token token = tokenizer.Next();

                while (token.Kind != TokenKind.End)
                {
                    // Every top-level item has the shape: name ( ... )
                    if (token.Kind != TokenKind.Word || tokenizer.Next().Kind != TokenKind.Open)
                    {
                        throw NotValid();
                    }

                    if (string.Equals(token.Value, "game", StringComparison.OrdinalIgnoreCase))
                    {
                        MetadataEntry entry = ReadGame(tokenizer);

                        if (entry != null)
                        {
                            entries.Add(entry);
                        }
                    }
                    else
                    {
                        SkipBlock(tokenizer);
                    }

                    token = tokenizer.Next();
                }
            }

            if (entries.Count == 0)
            {
                throw new InvalidDataException(Localizer.Format("metadata.error.noGames", Path.GetFileName(path)));
            }

            return entries;
        }

        private static MetadataEntry ReadGame(Tokenizer tokenizer)
        {
            string name = null;
            string comment = null;
            string crc = null;
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Token key = tokenizer.Next();

            while (key.Kind != TokenKind.Close)
            {
                if (key.Kind != TokenKind.Word)
                {
                    throw NotValid();
                }

                Token value = tokenizer.Next();

                if (value.Kind == TokenKind.Open)
                {
                    if (string.Equals(key.Value, "rom", StringComparison.OrdinalIgnoreCase))
                    {
                        crc = ReadRomChecksum(tokenizer) ?? crc;
                    }
                    else
                    {
                        SkipBlock(tokenizer);
                    }
                }
                else if (value.Kind == TokenKind.Text || value.Kind == TokenKind.Word)
                {
                    if (string.Equals(key.Value, "name", StringComparison.OrdinalIgnoreCase))
                    {
                        name = value.Value;
                    }
                    else if (string.Equals(key.Value, "comment", StringComparison.OrdinalIgnoreCase))
                    {
                        comment = value.Value;
                    }
                    else
                    {
                        fields[key.Value] = value.Value;
                    }
                }
                else
                {
                    throw NotValid();
                }

                key = tokenizer.Next();
            }

            string entryName = !string.IsNullOrWhiteSpace(name) ? name : comment;
            return string.IsNullOrWhiteSpace(entryName) ? null : new MetadataEntry(entryName.Trim(), crc, fields);
        }

        private static string ReadRomChecksum(Tokenizer tokenizer)
        {
            string crc = null;
            Token key = tokenizer.Next();

            while (key.Kind != TokenKind.Close)
            {
                if (key.Kind != TokenKind.Word)
                {
                    throw NotValid();
                }

                Token value = tokenizer.Next();

                if (value.Kind == TokenKind.Open)
                {
                    SkipBlock(tokenizer);
                }
                else if (value.Kind == TokenKind.Text || value.Kind == TokenKind.Word)
                {
                    if (string.Equals(key.Value, "crc", StringComparison.OrdinalIgnoreCase) && Crc32Pattern.IsMatch(value.Value))
                    {
                        crc = value.Value.ToUpperInvariant();
                    }
                }
                else
                {
                    throw NotValid();
                }

                key = tokenizer.Next();
            }

            return crc;
        }

        /// <summary>Skips everything up to the parenthesis that closes the current block (the opening one was already read).</summary>
        private static void SkipBlock(Tokenizer tokenizer)
        {
            int depth = 1;

            while (depth > 0)
            {
                Token token = tokenizer.Next();

                if (token.Kind == TokenKind.Open)
                {
                    depth++;
                }
                else if (token.Kind == TokenKind.Close)
                {
                    depth--;
                }
                else if (token.Kind == TokenKind.End)
                {
                    throw new InvalidDataException(Localizer.Get("metadata.error.endedUnexpectedly"));
                }
            }
        }

        private static InvalidDataException NotValid()
        {
            return new InvalidDataException(Localizer.Get("metadata.error.notValid"));
        }

        private enum TokenKind
        {
            Word,
            Text,
            Open,
            Close,
            End
        }

        private struct Token
        {
            public Token(TokenKind kind, string value)
            {
                Kind = kind;
                Value = value;
            }

            public TokenKind Kind { get; }

            public string Value { get; }
        }

        /// <summary>Splits the text into words, quoted texts and parentheses.</summary>
        private sealed class Tokenizer
        {
            private readonly TextReader _reader;

            public Tokenizer(TextReader reader)
            {
                _reader = reader;
            }

            public Token Next()
            {
                int next = _reader.Peek();

                while (next != -1 && char.IsWhiteSpace((char)next))
                {
                    _reader.Read();
                    next = _reader.Peek();
                }

                if (next == -1)
                {
                    return new Token(TokenKind.End, null);
                }

                if (next == '(')
                {
                    _reader.Read();
                    return new Token(TokenKind.Open, "(");
                }

                if (next == ')')
                {
                    _reader.Read();
                    return new Token(TokenKind.Close, ")");
                }

                var builder = new StringBuilder();

                if (next == '"')
                {
                    _reader.Read();

                    // Inside quotes everything is text, including parentheses.
                    while ((next = _reader.Read()) != -1 && next != '"')
                    {
                        builder.Append((char)next);
                    }

                    return new Token(TokenKind.Text, builder.ToString());
                }

                while (next != -1 && !char.IsWhiteSpace((char)next) && next != '(' && next != ')' && next != '"')
                {
                    builder.Append((char)_reader.Read());
                    next = _reader.Peek();
                }

                return new Token(TokenKind.Word, builder.ToString());
            }
        }
    }
}