namespace BosnetTest.Model.dto
{
    public class TransactionRequest
    {
        public string account { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
    }
}
