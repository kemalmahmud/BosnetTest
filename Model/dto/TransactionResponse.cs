namespace BosnetTest.Model.dto
{
    public class TransactionResponse
    {
        public string Status { get; set; }
        public string type { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string message { get; set; }
    }
}
