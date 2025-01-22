namespace BosnetTest.Model.dto
{
    public class TransactionRequest
    {
        public string account { get; set; }
        public List<TransactionRequestData> transactions { get; set;  }

        public class TransactionRequestData
        {
            public decimal amount { get; set; }
            public string currency { get; set; }
        }
    }
}
