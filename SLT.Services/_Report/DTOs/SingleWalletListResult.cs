using SLT.Domain.Collections;
using Utilities.Attributes;
using Utilities.DTOs;

namespace SLT.Services._Report.DTOs
{
    public class SingleWalletOrderListUpdate
    {
        public Pagination Pagination { get; set; } = new Pagination();
        [StringInputValidation(minLength: 20)] public string Wallet { get; set; }
        public OrderListType ListType { get; set; } = OrderListType.Received;
        public List<OrderState> States { get; set; } = [];

        public DateTime? FromTime { get; set; } = null;
        public DateTime? ToTime { get; set; } = null;

    }
    public class SingleWalletInvoiceListUpdate 
    {
        public Pagination Pagination { get; set; } = new Pagination();
        [StringInputValidation(minLength: 20)] public string Wallet { get; set; }
        public OrderListType ListType { get; set; } = OrderListType.Received;
        public List<InvoiceState> States { get; set; } = [];
        public DateTime? FromTime { get; set; } = null;
        public DateTime? ToTime { get; set; } = null;
    }
    public class GetAllOrdersUpdate 
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public List<OrderState> States { get; set; } = [];
        public DateTime? FromTime { get; set; } = null;
        public DateTime? ToTime { get; set; } = null;
    }
    public class GetAllInvoicesUpdate 
    { 
        public Pagination Pagination { get; set; } = new Pagination();
        public List<InvoiceState> States { get; set; } = [];
        public DateTime? FromTime { get; set; } = null;
        public DateTime? ToTime { get; set; } = null;
    }

    public enum OrderListType { Received, Sent }

}
