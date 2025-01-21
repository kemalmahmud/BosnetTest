using BosnetTest.Model.dto;
using System.Data.OleDb;
using System.Data;

namespace BosnetTest.Service
{
    public class CounterService
    {
        private readonly string _szCounterId = "001-COU";

        public int GetTransactionIdLastNumber(OleDbConnection connection, OleDbTransaction transaction)
        {
            int counter = -1;
            using (var command = new OleDbCommand($"SELECT [iLastNumber] FROM [BOS_Counter] where [szCounterId] = '{_szCounterId}'", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    counter = (int)(long)reader.GetValue(0);
                }
            }
            return counter;
        }
        public int GetAndUpdateTransactionIdLastNumber(int newLastNumber, OleDbConnection connection, OleDbTransaction transaction)
        {
            int counter = newLastNumber;

            using (var updateCommand = new OleDbCommand($"UPDATE [BOS_Counter] SET [iLastNumber] = {counter} WHERE [szCounterId] = '{_szCounterId}'", connection, transaction))
            {
                updateCommand.ExecuteNonQuery();
            }

            return counter;
        }

        public string CreateTransactionId(int lastNumber)
        {
            // memastikan angkanya 10 digit
            if (lastNumber < 0 || lastNumber > 9999999999)
            {
                throw new ArgumentOutOfRangeException(nameof(lastNumber), "Value must be between 0 and 9,999,999,999.");
            }

            string currentDate = DateTime.Now.ToString("yyyyMMdd");
            string formattedNumber = $"{lastNumber / 100000:D5}.{lastNumber % 100000:D5}";

            // tanggal dan counter
            string transactionId = $"{currentDate}-{formattedNumber}";

            return transactionId;
        }
    }
}

