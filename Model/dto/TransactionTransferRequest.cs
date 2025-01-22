namespace BosnetTest.Model.dto
{
    public class TransactionTransferRequest
    {
        public string accountFrom { get; set; }
        public List<TransferDataRequest> transfers { get; set; }

        public class TransferDataRequest
        {
            public string accountTo { get; set; } 
            public decimal amount { get; set; } 
            public string currency { get; set; } 
        }
    }

}
