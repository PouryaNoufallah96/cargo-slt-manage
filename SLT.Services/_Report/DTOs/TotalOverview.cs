namespace SLT.Services._Report.DTOs
{
    public class TotalOverview
    {
        public int TotalOrderCount { get; set; }
        public decimal TotalOrderAmountInUsdt { get; set; } 
        public int PendingOrderCount { get; set; }
        public decimal PendingOrderAmountInUsdt { get; set; }
        public int DoneOrderCount { get; set; }
        public decimal DoneOrderAmountInUsdt { get; set; }
        public decimal OrderProgress { get; set; } 

        public int TotalInvoiceCount { get; set; }
        public decimal TotalInvoiceAmountInUsdt { get; set; }
        public int PaidInvoiceCount { get; set; }
        public decimal PaidInvoiceAmountInUsdt { get; set; }
        public int PendingInvoiceCount { get; set; }
        public decimal PendingInvoiceAmountInUsdt { get; set; }
        public decimal InvoiceProgress { get; set; } 
    }
}
