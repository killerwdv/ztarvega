using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZtarVega.Services
{
    /// <summary>
    /// Holds the entries of the metadata files and finds the metadata of a game by its name. Each distinct game
    /// has one record, identified by its spaced name, so different games with similar names are never mixed.
    /// When a game has no entry of its own for a field, the other versions of the same title can supply it, but
    /// only when they do not contradict each other.
    /// </summary>
    internal sealed class MetadataIndex
    {
        private const string DeveloperField = "developer";
        private const string GenreField = "genre";
        private const string ReleaseYearField = "releaseyear";

        private const int MinimumYear = 1970;
        private const int MaximumYear = 2100;

        // One record per distinct game.
        private readonly Dictionary<string, MetadataRecord> _recordsBySpacedKey = new Dictionary<string, MetadataRecord>();

        // The same records grouped by the simplified name: a fallback for names that differ only in how a
        // punctuation mark is written ("Spider-Man" and "Spiderman").
        private readonly Dictionary<string, List<MetadataRecord>> _recordsBySimplifiedKey = new Dictionary<string, List<MetadataRecord>>();

        // The records of all the versions of a title.
        private readonly Dictionary<string, List<MetadataRecord>> _recordsBySpacedTitleKey = new Dictionary<string, List<MetadataRecord>>();
        private readonly Dictionary<string, List<MetadataRecord>> _recordsBySimplifiedTitleKey = new Dictionary<string, List<MetadataRecord>>();

        /// <summary>Number of entries read from the files.</summary>
        public int EntryCount { get; private set; }

        /// <summary>Adds the entries of a file. The fields of the same game coming from different files are combined.</summary>
        public void Add(IEnumerable<MetadataEntry> entries)
        {
            foreach (MetadataEntry entry in entries)
            {
                EntryCount++;

                string spacedKey = NameKey.SpacedFull(entry.Name);

                if (spacedKey.Length == 0)
                {
                    continue;
                }

                MetadataRecord record;

                if (!_recordsBySpacedKey.TryGetValue(spacedKey, out record))
                {
                    record = new MetadataRecord(NameKey.SpacedTitle(entry.Name));
                    _recordsBySpacedKey.Add(spacedKey, record);

                    AddToGroup(_recordsBySimplifiedKey, NameKey.Full(entry.Name), record);
                    AddToGroup(_recordsBySpacedTitleKey, record.SpacedTitleKey, record);
                    AddToGroup(_recordsBySimplifiedTitleKey, NameKey.Title(entry.Name), record);
                }

                foreach (KeyValuePair<string, string> field in entry.Fields)
                {
                    if (!string.IsNullOrWhiteSpace(field.Value))
                    {
                        record.Fields[field.Key] = field.Value.Trim();
                    }
                }
            }
        }

        /// <summary>Returns the metadata of the game with the given title, or null when the files have nothing for it.</summary>
        public GameMetadata Find(string title)
        {
            MetadataRecord exact = FindExact(title);
            List<MetadataRecord> sameTitle = FindSameTitle(title);

            // The other versions of the title can only complete the data when they do not contradict each other:
            // two different developers mean two different games that happen to share a title.
            int developerCount = sameTitle == null ? 0 : DistinctValues(sameTitle, DeveloperField).Count;
            bool canUseSameTitle = sameTitle != null && developerCount <= 1;

            string developer = exact?.GetField(DeveloperField);
            string genre = exact?.GetField(GenreField);
            int? year = ParseYear(exact?.GetField(ReleaseYearField));

            if (canUseSameTitle)
            {
                developer = developer ?? Unanimous(sameTitle, DeveloperField);
                genre = genre ?? Unanimous(sameTitle, GenreField);

                if (!year.HasValue)
                {
                    // With a known developer, the oldest year is the original release. Without one, a year is
                    // only used when every version of the title agrees on it.
                    year = developerCount == 1 ? EarliestYear(sameTitle) : UnanimousYear(sameTitle);
                }
            }

            var metadata = new GameMetadata(year, developer, genre);
            return metadata.HasData ? metadata : null;
        }

        private MetadataRecord FindExact(string title)
        {
            MetadataRecord record;

            if (_recordsBySpacedKey.TryGetValue(NameKey.SpacedFull(title), out record))
            {
                return record;
            }

            // The names may differ in how a punctuation mark is written: the simplified name decides,
            // as long as it points to a single record.
            List<MetadataRecord> candidates;

            if (_recordsBySimplifiedKey.TryGetValue(NameKey.Full(title), out candidates) && candidates.Count == 1)
            {
                return candidates[0];
            }

            return null;
        }

        private List<MetadataRecord> FindSameTitle(string title)
        {
            List<MetadataRecord> records;

            if (_recordsBySpacedTitleKey.TryGetValue(NameKey.SpacedTitle(title), out records))
            {
                return records;
            }

            // Same idea as above: the simplified title is only used when it belongs to a single title.
            if (_recordsBySimplifiedTitleKey.TryGetValue(NameKey.Title(title), out records) &&
                records.Select(record => record.SpacedTitleKey).Distinct().Count() == 1)
            {
                return records;
            }

            return null;
        }

        private static void AddToGroup(Dictionary<string, List<MetadataRecord>> groups, string key, MetadataRecord record)
        {
            if (key.Length == 0)
            {
                return;
            }

            List<MetadataRecord> group;

            if (!groups.TryGetValue(key, out group))
            {
                group = new List<MetadataRecord>();
                groups.Add(key, group);
            }

            group.Add(record);
        }

        private static List<string> DistinctValues(IEnumerable<MetadataRecord> records, string field)
        {
            return records
                .Select(record => record.GetField(field))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>The value of the field when every record that has one agrees; otherwise null.</summary>
        private static string Unanimous(IEnumerable<MetadataRecord> records, string field)
        {
            List<string> values = DistinctValues(records, field);
            return values.Count == 1 ? values[0] : null;
        }

        /// <summary>The oldest release year among the records: the year of the original release.</summary>
        private static int? EarliestYear(IEnumerable<MetadataRecord> records)
        {
            int? earliest = null;

            foreach (MetadataRecord record in records)
            {
                int? year = ParseYear(record.GetField(ReleaseYearField));

                if (year.HasValue && (!earliest.HasValue || year.Value < earliest.Value))
                {
                    earliest = year;
                }
            }

            return earliest;
        }

        /// <summary>The release year when every record that has one agrees on it; otherwise null.</summary>
        private static int? UnanimousYear(IEnumerable<MetadataRecord> records)
        {
            List<int> years = records
                .Select(record => ParseYear(record.GetField(ReleaseYearField)))
                .Where(year => year.HasValue)
                .Select(year => year.Value)
                .Distinct()
                .ToList();

            return years.Count == 1 ? years[0] : (int?)null;
        }

        private static int? ParseYear(string text)
        {
            int year;

            if (text != null &&
                int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out year) &&
                year >= MinimumYear && year <= MaximumYear)
            {
                return year;
            }

            return null;
        }

        /// <summary>The fields known for one game, combined from every file.</summary>
        private sealed class MetadataRecord
        {
            public MetadataRecord(string spacedTitleKey)
            {
                SpacedTitleKey = spacedTitleKey;
            }

            /// <summary>The key of the title of the game, shared by all its versions.</summary>
            public string SpacedTitleKey { get; }

            public Dictionary<string, string> Fields { get; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public string GetField(string field)
            {
                string value;
                return Fields.TryGetValue(field, out value) ? value : null;
            }
        }
    }
}