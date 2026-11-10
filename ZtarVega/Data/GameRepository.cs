using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using ZtarVega.Models;

namespace ZtarVega.Data
{
    /// <summary>SQLite implementation of <see cref="IGameRepository"/>.</summary>
    internal sealed class GameRepository : IGameRepository
    {
        private const string SelectColumns =
            "Id, Title, Year, RomType, Region, Language, Tags, Developer, Players, " +
            "RomFilename, Crc32, IsAvailable, IsFavorite, LastPlayed, PlayCount, BaseGameId";

        private const string InsertSql = @"
            INSERT INTO Games (Title, Year, RomType, Region, Language, Tags, Developer, Players,
                               RomFilename, Crc32, IsAvailable, IsFavorite, LastPlayed, PlayCount, BaseGameId)
            VALUES (@Title, @Year, @RomType, @Region, @Language, @Tags, @Developer, @Players,
                    @RomFilename, @Crc32, @IsAvailable, @IsFavorite, @LastPlayed, @PlayCount, @BaseGameId);";

        private const string DateFormat = "o"; // ISO 8601, round-trippable.

        private readonly DatabaseConnectionFactory _connectionFactory;

        public GameRepository(DatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public IReadOnlyList<Game> GetAll()
        {
            var games = new List<Game>();

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(
                "SELECT " + SelectColumns + " FROM Games ORDER BY Title COLLATE NOCASE;", connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    games.Add(MapGame(reader));
                }
            }

            return games;
        }

        public Game GetById(int id)
        {
            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(
                "SELECT " + SelectColumns + " FROM Games WHERE Id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapGame(reader) : null;
                }
            }
        }

        public int Add(Game game)
        {
            ValidateGame(game);

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(InsertSql + " SELECT last_insert_rowid();", connection))
            {
                AddGameParameters(command, game);

                int newId = Convert.ToInt32(command.ExecuteScalar());
                game.Id = newId;
                return newId;
            }
        }

        public void AddRange(IEnumerable<Game> games)
        {
            if (games == null)
            {
                throw new ArgumentNullException(nameof(games));
            }

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            using (var command = new SQLiteCommand(InsertSql, connection, transaction))
            {
                foreach (Game game in games)
                {
                    ValidateGame(game);

                    command.Parameters.Clear();
                    AddGameParameters(command, game);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void Update(Game game)
        {
            ValidateGame(game);

            const string sql = @"
                UPDATE Games SET
                    Title = @Title, Year = @Year, RomType = @RomType, Region = @Region,
                    Language = @Language, Tags = @Tags, Developer = @Developer, Players = @Players,
                    RomFilename = @RomFilename, Crc32 = @Crc32, IsAvailable = @IsAvailable,
                    IsFavorite = @IsFavorite, LastPlayed = @LastPlayed, PlayCount = @PlayCount, BaseGameId = @BaseGameId
                WHERE Id = @Id;";

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(sql, connection))
            {
                AddGameParameters(command, game);
                command.Parameters.AddWithValue("@Id", game.Id);

                if (command.ExecuteNonQuery() == 0)
                {
                    throw new InvalidOperationException("The game with id " + game.Id + " was not found.");
                }
            }
        }

        public void Delete(int id)
        {
            ExecuteById("DELETE FROM Games WHERE Id = @Id;", id);
        }

        public void SetFavorite(int id, bool isFavorite)
        {
            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(
                "UPDATE Games SET IsFavorite = @IsFavorite WHERE Id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@IsFavorite", isFavorite ? 1 : 0);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        public void RegisterPlay(int id)
        {
            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(
                "UPDATE Games SET LastPlayed = @LastPlayed, PlayCount = PlayCount + 1 WHERE Id = @Id;",
                connection))
            {
                command.Parameters.AddWithValue("@LastPlayed", FormatDate(DateTime.UtcNow));
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        public void UpdateRomInfo(IEnumerable<GameRomUpdate> updates)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            const string sql = @"
                UPDATE Games
                SET RomFilename = @RomFilename, Crc32 = @Crc32, IsAvailable = @IsAvailable
                WHERE Id = @Id;";

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            using (var command = new SQLiteCommand(sql, connection, transaction))
            {
                foreach (GameRomUpdate update in updates)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@RomFilename", ToDbValue(update.RomFilename));
                    command.Parameters.AddWithValue("@Crc32", ToDbValue(update.Crc32));
                    command.Parameters.AddWithValue("@IsAvailable", update.IsAvailable ? 1 : 0);
                    command.Parameters.AddWithValue("@Id", update.GameId);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void UpdateMetadata(IEnumerable<GameMetadataUpdate> updates, bool replaceExisting)
        {
            if (updates == null)
            {
                throw new ArgumentNullException(nameof(updates));
            }

            // A null parameter means "nothing to change for this field".
            // Fill mode writes only into empty fields (the check happens inside the database itself);
            // replace mode overwrites a field whenever a new value is given.
            const string fillEmptySql = @"
                UPDATE Games SET
                    Year = COALESCE(Year, @Year),
                    Developer = COALESCE(NULLIF(Developer, ''), @Developer),
                    Tags = COALESCE(NULLIF(Tags, ''), @Tags)
                WHERE Id = @Id;";

            const string replaceSql = @"
                UPDATE Games SET
                    Year = COALESCE(@Year, Year),
                    Developer = COALESCE(@Developer, Developer),
                    Tags = COALESCE(@Tags, Tags)
                WHERE Id = @Id;";

            string sql = replaceExisting ? replaceSql : fillEmptySql;

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            using (var command = new SQLiteCommand(sql, connection, transaction))
            {
                foreach (GameMetadataUpdate update in updates)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@Year", ToDbValue(update.Year));
                    command.Parameters.AddWithValue("@Developer", ToDbValue(update.Developer));
                    command.Parameters.AddWithValue("@Tags", ToDbValue(update.Tags));
                    command.Parameters.AddWithValue("@Id", update.GameId);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        public void SetBaseGames(IEnumerable<GameLink> links)
        {
            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }

            // The check happens inside the database: a game that already has a base game is not touched.
            const string sql = "UPDATE Games SET BaseGameId = @BaseGameId WHERE Id = @Id AND BaseGameId IS NULL;";

            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (SQLiteTransaction transaction = connection.BeginTransaction())
            using (var command = new SQLiteCommand(sql, connection, transaction))
            {
                foreach (GameLink link in links)
                {
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@BaseGameId", link.BaseGameId);
                    command.Parameters.AddWithValue("@Id", link.GameId);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }
        }

        private void ExecuteById(string sql, int id)
        {
            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SQLiteCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        private static void ValidateGame(Game game)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            if (string.IsNullOrWhiteSpace(game.Title))
            {
                throw new ArgumentException("A game must have a title.", nameof(game));
            }
        }

        private static void AddGameParameters(SQLiteCommand command, Game game)
        {
            SQLiteParameterCollection parameters = command.Parameters;

            parameters.AddWithValue("@Title", game.Title);
            parameters.AddWithValue("@Year", ToDbValue(game.Year));
            parameters.AddWithValue("@RomType", game.RomType.ToString());
            parameters.AddWithValue("@Region", game.Region.ToString());
            parameters.AddWithValue("@Language", ToDbValue(game.Language));
            parameters.AddWithValue("@Tags", ToDbValue(game.Tags));
            parameters.AddWithValue("@Developer", ToDbValue(game.Developer));
            parameters.AddWithValue("@Players", ToDbValue(game.Players));
            parameters.AddWithValue("@RomFilename", ToDbValue(game.RomFilename));
            parameters.AddWithValue("@Crc32", ToDbValue(game.Crc32));
            parameters.AddWithValue("@IsAvailable", game.IsAvailable ? 1 : 0);
            parameters.AddWithValue("@IsFavorite", game.IsFavorite ? 1 : 0);
            parameters.AddWithValue("@LastPlayed", game.LastPlayed.HasValue ? FormatDate(game.LastPlayed.Value) : (object)DBNull.Value);
            parameters.AddWithValue("@PlayCount", game.PlayCount);
            parameters.AddWithValue("@BaseGameId", ToDbValue(game.BaseGameId));
        }

        private static Game MapGame(SQLiteDataReader reader)
        {
            return new Game
            {
                Id = GetInt(reader, "Id"),
                Title = GetString(reader, "Title"),
                Year = GetNullableInt(reader, "Year"),
                RomType = ParseEnum(GetString(reader, "RomType"), RomType.Original),
                Region = ParseEnum(GetString(reader, "Region"), GameRegion.World),
                Language = GetNullableString(reader, "Language"),
                Tags = GetNullableString(reader, "Tags"),
                Developer = GetNullableString(reader, "Developer"),
                Players = GetNullableInt(reader, "Players"),
                RomFilename = GetNullableString(reader, "RomFilename"),
                Crc32 = GetNullableString(reader, "Crc32"),
                IsAvailable = GetInt(reader, "IsAvailable") != 0,
                IsFavorite = GetInt(reader, "IsFavorite") != 0,
                LastPlayed = ParseNullableDate(GetNullableString(reader, "LastPlayed")),
                PlayCount = GetInt(reader, "PlayCount"),
                BaseGameId = GetNullableInt(reader, "BaseGameId")
            };
        }

        private static object ToDbValue(object value)
        {
            return value ?? DBNull.Value;
        }

        private static string FormatDate(DateTime value)
        {
            return value.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        private static DateTime? ParseNullableDate(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static TEnum ParseEnum<TEnum>(string text, TEnum fallback) where TEnum : struct
        {
            TEnum value;
            return Enum.TryParse(text, true, out value) ? value : fallback;
        }

        private static int GetInt(SQLiteDataReader reader, string columnName)
        {
            return reader.GetInt32(reader.GetOrdinal(columnName));
        }

        private static int? GetNullableInt(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        private static string GetString(SQLiteDataReader reader, string columnName)
        {
            return reader.GetString(reader.GetOrdinal(columnName));
        }

        private static string GetNullableString(SQLiteDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}