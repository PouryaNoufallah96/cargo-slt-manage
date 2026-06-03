using SLT.Domain.Collections;

namespace SLT.Services._Report.DTOs
{
    public class OrderFullResult : OrderResult
    {
        public List<InvoiceResult> Invoices { get; set; }
        public OwnershipType? OwnershipType { get; set; }
    }
    public enum OwnershipType { Owner, Payer }

    public class InvoiceListResult
    {
        public List<InvoiceResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }

    public class InvoiceResult
    {
        public DateTime CreatedMoment { get; set; }
        public DateTime? ModifiedMoment { get; set; }
        public string InvoiceId { get; set; }
        public string OwnerWallet { get; set; }
        public string PayerWallet { get; set; }
        public string OrderId { get; set; }
        public string TokenSymbol { get; set; }
        public string TokenNetwork { get; set; } 
        public string TokenAddress { get; set; }
        public decimal USDTAmount { get; set; }
        public string USDTAmountInWei { get; set; }
        public string Desctiption { get; set; }

        public decimal? TokenAmountAtPayment { get; set; }
        public string? TokenAmountWeiAtPayment { get; set; }
        public decimal? TokenPriceAtPayment { get; set; }

        public InvoiceState State { get; set; } 
        public DateTime? PayMoment { get; set; } = null;
        public string RegisterHash { get; set; } = null;
        public string PaymentHash { get; set; } = null;
        public DateTime? ActivateDate { get; set; } = null;
        public OwnershipType? OwnershipType { get; set; }
    }

    public class OrderListResult
    {
        public List<OrderResult> Data { get; set; } = [];
        public int PageCount { get; set; } = 0;
        public int TotalCount { get; set; } = 0;
    }

    public class OrderResult
    {
        public DateTime CreatedMoment { get; set; }
        public DateTime? ModifiedMoment { get; set; }
        public string OrderId { get; set; }
        public string TransferId { get; set; }

        public string OwnerWallet { get; set; }
        public string PayerWallet { get; set; }
        public List<string> SeenBy { get; set; }

        public decimal TotalAmount { get; set; }
        public string Transportation { get; set; }
        public OrderType Type { get; set; }
        public OrderState State { get; set; }
        public DateTime? PaymentDay { get; set; }

    }




    

}
