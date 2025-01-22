namespace BosnetTest.Model.dto
{
    public class TransactionHistoryResponse
    {
        public List<TransactionHistoryData> histories { get; set; }
        public string status { get; set; }
        public string message { get; set; }

        public class TransactionHistoryData
        {
            public string account { get; set; }
            public string transactionType { get; set; }
            public decimal amount { get; set; }
            public string currency { get; set; }
            public string date { get; set; }
        }
    }
}
