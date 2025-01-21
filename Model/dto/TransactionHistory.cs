namespace BosnetTest.Model.dto
{
    public class TransactionHistory
    {
        public string account {  get; set; }
        public string transactionType { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string date { get; set; }
    }
}
