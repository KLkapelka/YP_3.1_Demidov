using Npgsql;
using System;

namespace ProjectManagementApp
{
    public static class DatabaseHelper
    {
        // Метод для выполнения SQL-запросов без возврата данных (INSERT, UPDATE, DELETE)
        public static void ExecuteNonQuery(string query)
        {
            using (var conn = new NpgsqlConnection(App.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Метод для выполнения SQL-запросов с возвратом данных (SELECT)
        public static NpgsqlDataReader ExecuteQuery(string query)
        {
            var conn = new NpgsqlConnection(App.ConnectionString);
            conn.Open();
            var cmd = new NpgsqlCommand(query, conn);
            return cmd.ExecuteReader();
        }
    }
}