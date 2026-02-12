using SLT.Services._Report.DTOs;

namespace SLT.Services._Report
{
    public interface IReportService
    {

        // hash base 
        Task<OrderFullResult> GetSingleHashInvoiceDataAsync(HashUpdate update);

        // order or transfer id base
        Task<OrderFullResult> GetOrderDetailAsync(OrderIdUpdate update);


        // wallet base
        Task<SingleWalletOverview> GetSingleWalletOverviewAsync(WalletUpdate update);
        Task<OrderListResult> GetSingleWalletOrdersAsync(SingleWalletOrderListUpdate update);
        Task<InvoiceListResult> GetSingleWalletInvoicesAsync(SingleWalletInvoiceListUpdate update);


        // total side 
        Task<TotalOverview> GetTotalOverviewAsync();
        Task<OrderListResult> GetAllOrdersAsync(GetAllOrdersUpdate update);
        Task<InvoiceListResult> GetAllInvoicesAsync(GetAllInvoicesUpdate update);

    }
}
 