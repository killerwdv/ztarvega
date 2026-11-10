using System;
using System.Data.SQLite;

namespace ZtarVega.Data
{
    /// <summary>Creates the database schema and applies pending migrations.</summary>
    internal sealed class DatabaseInitializer
    {
        private const int LatestSchemaVersion = 2;

        private readonly DatabaseConnectionFactory _connectionFactory;

        public DatabaseInitializer(DatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        /// <summary>Brings the database up to the latest schema version.</summary>
        public void Initialize()
        {
            using (SQLiteConnection connection = _connectionFactory.CreateOpenConnection())
            {
                int currentVersion = GetSchemaVersion(connection);

                if (currentVersion >= LatestSchemaVersion)
                {
                    return;
                }

                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    if (currentVersion < 1)
                    {
                        MigrateToVersion1(connection, transaction);
                    }

                    if (currentVersion < 2)
                    {
                        MigrateToVersion2(connection, transaction);
                    }

                    SetSchemaVersion(connection, transaction, LatestSchemaVersion);
                    transaction.Commit();
                }
            }
        }

        private static void MigrateToVersion1(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            const string createGamesTable = @"
                CREATE TABLE Games (
                    Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title        TEXT    NOT NULL,
                    Year         INTEGER,
                    RomType      TEXT    NOT NULL DEFAULT 'Original',
                    Region       TEXT    NOT NULL DEFAULT 'World',
                    Language     TEXT,
                    Tags         TEXT,
                    Developer    TEXT,
                    Players      INTEGER,
                    RomFilename  TEXT,
                    Crc32        TEXT,
                    IsAvailable  INTEGER NOT NULL DEFAULT 0,
                    IsFavorite   INTEGER NOT NULL DEFAULT 0,
                    LastPlayed   TEXT,
                    PlayCount    INTEGER NOT NULL DEFAULT 0
                );";

            ExecuteNonQuery(connection, transaction, createGamesTable);
            ExecuteNonQuery(connection, transaction, "CREATE INDEX IX_Games_Title ON Games (Title COLLATE NOCASE);");
            ExecuteNonQuery(connection, transaction, "CREATE INDEX IX_Games_Crc32 ON Games (Crc32);");
            ExecuteNonQuery(connection, transaction, "CREATE INDEX IX_Games_LastPlayed ON Games (LastPlayed);");
        }

        private static void MigrateToVersion2(SQLiteConnection connection, SQLiteTransaction transaction)
        {
            // A game can be based on another one (a translation or hack of an original). When the base game
            // is deleted, the link is cleared instead of leaving a reference to a game that no longer exists.
            ExecuteNonQuery(
                connection,
                transaction,
                "ALTER TABLE Games ADD COLUMN BaseGameId INTEGER REFERENCES Games (Id) ON DELETE SET NULL;");
            ExecuteNonQuery(connection, transaction, "CREATE INDEX IX_Games_BaseGameId ON Games (BaseGameId);");
        }

        private static int GetSchemaVersion(SQLiteConnection connection)
        {
            using (var command = new SQLiteCommand("PRAGMA user_version;", connection))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void SetSchemaVersion(SQLiteConnection connection, SQLiteTransaction transaction, int version)
        {
            // PRAGMA statements do not accept parameters; the value is an int we control, so this is safe.
            ExecuteNonQuery(connection, transaction, "PRAGMA user_version = " + version + ";");
        }

        private static void ExecuteNonQuery(SQLiteConnection connection, SQLiteTransaction transaction, string sql)
        {
            using (var command = new SQLiteCommand(sql, connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }
    }
}