namespace SLT.Services._Report.DTOs
{
  
    public class SingleWalletOverview
    {
        public SingleWalletOverviewResult WalletAsOwnerReport { get; set; }
        public SingleWalletOverviewResult WalletAsPayerReport { get; set; }
    }

    public class SingleWalletOverviewResult
    {
        public int TotalOrderCount { get; set; }
        public int PendingOrderCount { get; set; }
        public int DoneOrderCount { get; set; }
        public decimal OrderProgress { get; set; }

        public int TotalInvoiceCount { get; set; }
        public int PaidInvoiceCount { get; set; }
        public int PendingInvoiceCount { get; set; }
        public decimal InvoiceProgress { get; set; }
    }





}
