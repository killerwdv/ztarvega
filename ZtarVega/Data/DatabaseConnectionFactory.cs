using System;
using System.Data.SQLite;

namespace ZtarVega.Data
{
    /// <summary>Creates open connections to the application's SQLite database.</summary>
    internal sealed class DatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public DatabaseConnectionFactory(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("The database path cannot be empty.", nameof(databasePath));
            }

            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databasePath,
                Version = 3,
                ForeignKeys = true
            };

            _connectionString = builder.ConnectionString;
        }

        /// <summary>Creates and opens a new connection. The caller must dispose it.</summary>
        public SQLiteConnection CreateOpenConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}