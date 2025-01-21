namespace BosnetTest.Model
{
    public class BOS_History
    {
        public string szTransactionId {  get; set; }
        public string szAccountId { get; set; }
        public string szCurrencyId { get; set; }
        public DateTime dtmTransaction { get; set; }
        public decimal decAmount { get; set; }
        public string szNote { get; set; }
    }
}
