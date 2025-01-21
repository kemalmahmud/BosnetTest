using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using Microsoft.Extensions.Configuration;

namespace BosnetTest.Service
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OleDbConnection");
        }

        public List<Dictionary<string, object>> GetAllData()
        {
            var results = new List<Dictionary<string, object>>();

            using (var connection = new OleDbConnection(_connectionString))
            {
                connection.Open();

                // Mulai transaksi dengan isolation level SNAPSHOT
                using (var transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                {
                    try
                    {
                        using (var command = new OleDbCommand("SELECT * FROM [user]", connection, transaction))
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                }
                                results.Add(row);
                            }
                        }

                        // Commit transaksi jika berhasil
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback jika terjadi kesalahan
                        transaction.Rollback();
                        Console.WriteLine($"Error: {ex.Message}");
                        throw;
                    }
                }
            }

            return results;
        }
    }
}
