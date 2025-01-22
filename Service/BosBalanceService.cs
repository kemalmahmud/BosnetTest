using BosnetTest.Model;
using System.Data.OleDb;
using System.Globalization;
using System.Transactions;

namespace BosnetTest.Service
{
    public class BosBalanceService
    {
        public string UpdateBosBalance(BOS_Balance request, String type, OleDbConnection connection, OleDbTransaction transaction)
        {
            var getstring = $"SELECT * FROM [BOS_Balance] where [szAccountId] = '{request.szAccountId}' and [szCurrencyId] = '{request.szCurrencyId}'";
            using (var command = new OleDbCommand(getstring, connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                // insert account baru
                if (!reader.HasRows)
                {
                    if (request.decAmount < 0) throw new Exception("Saldo tidak mencukupi");
                    string newBalanceString = request.decAmount.ToString("0.00000000").Replace(',', '.');
                    var stringCommand = $"INSERT INTO [BOS_Balance] VALUES ('{request.szAccountId}', '{request.szCurrencyId}', {newBalanceString})";
                    using (var insertCommand = new OleDbCommand(stringCommand, connection, transaction))
                    {
                        insertCommand.ExecuteNonQuery();
                    }
                }
                // update account lama
                else
                {
                    decimal oldBalance = 0;
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        oldBalance = decimal.Parse(row["decAmount"].ToString());
                    }
                    // ubah balance
                    decimal newBalance = 0;
                    if(type.Contains("SETOR")) newBalance = oldBalance + request.decAmount;
                    else newBalance = oldBalance - request.decAmount;

                    if (newBalance < 0) throw new Exception("saldo tidak mencukupi");
                    string newBalanceString = newBalance.ToString("0.00000000").Replace(',', '.');

                    var updatestring = $"UPDATE [BOS_Balance] SET [decAmount] = {newBalanceString} where [szAccountId] = '{request.szAccountId}' and [szCurrencyId] = '{request.szCurrencyId}'";
                    using (var updateCommand = new OleDbCommand(updatestring, connection, transaction))
                    {
                        updateCommand.ExecuteNonQuery();
                    }
                }
                return "Success";
            }
        }
    }
}
