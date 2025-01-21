namespace BosnetTest.Model.dto
{
    public class TransactionTransferRequest
    {
        public string AccountFrom { get; set; }
        public List<TransferContentRequest> Transfers { get; set; }

        public class TransferContentRequest
        {
            public string AccountTo { get; set; } 
            public decimal Amount { get; set; } 
            public string Currency { get; set; } 
        }
    }

}
