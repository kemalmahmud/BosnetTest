namespace BosnetTest.Model.dto
{
    public class TransactionHistoryResponse
    {
        public List<TransactionHistoryData> histories { get; set; }
        public string status { get; set; }
        public string message { get; set; }

        public class TransactionHistoryData
        {
            public string Account { get; set; }
            public string TransactionType { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public string Date { get; set; }
        }
    }
}
