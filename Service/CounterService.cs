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
        public int GetAndUpdateTransactionIdLastNumber(Boolean reset, OleDbConnection connection, OleDbTransaction transaction)
        {
            int counter = 0;
            if(!reset) counter = GetTransactionIdLastNumber(connection, transaction);

            // Jika nilai iLastNumber ditemukan, tingkatkan nilainya
            if (counter != -1)
            {
                counter++;

                using (var updateCommand = new OleDbCommand($"UPDATE [BOS_Counter] SET [iLastNumber] = {counter} WHERE [szCounterId] = '{_szCounterId}'", connection, transaction))
                {
                    updateCommand.ExecuteNonQuery();
                }
            }
            else
            {
                throw new InvalidOperationException("szCounterId not found in BOS_Counter.");
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

