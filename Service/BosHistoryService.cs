using BosnetTest.Model;
using System.Data.OleDb;
using System.Data;
using System.Transactions;

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
    }
}
