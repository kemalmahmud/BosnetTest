using BosnetTest.Model;
using System.Data.OleDb;
using System.Data;
using System.Transactions;
using BosnetTest.Model.dto;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace BosnetTest.Service
{
    public class BosHistoryService
    {
        public string InsertToBosHistory(BOS_History request, OleDbConnection connection, OleDbTransaction transaction)
        {
            var dateTimeForSql = request.dtmTransaction.ToString("yyyy-MM-dd HH:mm:ss");
            var stringCommand = $"INSERT INTO [BOS_History] VALUES ('{request.szTransactionId}', '{request.szAccountId}', '{request.szCurrencyId}', '{dateTimeForSql}', {request.decAmount}, '{request.szNote}')";
            using (var updateCommand = new OleDbCommand(stringCommand, connection, transaction))
            {
                updateCommand.ExecuteNonQuery();
            }
            return "Success";
        }

        public List<Dictionary<string, object>> GetTransactionHistory(TransactionHistoryRequest request, OleDbConnection connection, OleDbTransaction transaction)
        {
            var results = new List<Dictionary<string, object>>();

            // Membuat query dinamis
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(request.Account))
            {
                conditions.Add($"szAccountId = '{request.Account}'");
            }
            if (!string.IsNullOrEmpty(request.DateFrom))
            {
                var dateFrom = request.DateFrom + " 00:00:00";
                conditions.Add($"dtmTransaction >= '{dateFrom}'");
            }
            if (!string.IsNullOrEmpty(request.DateTo))
            {
                var dateTo = request.DateTo + " 23:59:59";
                conditions.Add($"dtmTransaction <= '{dateTo}'");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
            var commandstring = $"SELECT * FROM [BOS_History] {whereClause}";

            using (var command = new OleDbCommand(commandstring, connection, transaction))
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
            return results;
        }
    }
}
