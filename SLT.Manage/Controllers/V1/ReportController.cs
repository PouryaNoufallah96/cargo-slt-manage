using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SLT.Services._Report;
using SLT.Services._Report.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using Utilities.Api;
using Utilities.Attributes;
using Utilities.Filters;
using Utilities.Permissions;

namespace SLT.Manage.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController(IReportService _reportService) : ApiBaseController
    {
        #region Hash Base

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 50)]
        [SwaggerOperation(
            Summary = "Get full invoice and order data using hash",
            Description = "Returns complete invoice and related order details based on unique hash identifier.",
            Tags = ["Report - Hash Base"]
        )]
        public async Task<OrderFullResult> GetSingleHashInvoiceData(
            [FromBody] HashUpdate update)
            => await _reportService.GetSingleHashInvoiceDataAsync(update);


        #endregion


        #region Order / Transfer Base

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 50)]
        [SwaggerOperation(
            Summary = "Get order detail by order or transfer id",
            Description = "Returns complete order information including invoices and transaction data using orderId or transferId.",
            Tags = ["Report - Order Base"]
        )]
        public async Task<OrderFullResult> GetOrderDetail(
            [FromBody] OrderIdUpdate update)
            => await _reportService.GetOrderDetailAsync(update);


        #endregion


        #region Wallet Base

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 100)]
        [SwaggerOperation(
            Summary = "Get single wallet overview",
            Description = "Returns wallet balance, summary statistics, and general financial overview for a specific wallet.",
            Tags = ["Report - Wallet"]
        )]
        public async Task<SingleWalletOverview> GetSingleWalletOverview(
            [FromBody] WalletUpdate update)
            => await _reportService.GetSingleWalletOverviewAsync(update);


        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 100)]
        [SwaggerOperation(
            Summary = "Get wallet orders list",
            Description = "Returns paginated list of orders related to a specific wallet.",
            Tags = ["Report - Wallet"]
        )]
        public async Task<OrderListResult> GetSingleWalletOrders(
            [FromBody] SingleWalletOrderListUpdate update)
            => await _reportService.GetSingleWalletOrdersAsync(update);


        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 100)]
        [SwaggerOperation(
            Summary = "Get wallet invoices list",
            Description = "Returns paginated list of invoices related to a specific wallet.",
            Tags = ["Report - Wallet"]
        )]
        public async Task<InvoiceListResult> GetSingleWalletInvoices(
            [FromBody] SingleWalletInvoiceListUpdate update)
            => await _reportService.GetSingleWalletInvoicesAsync(update);


        #endregion


        #region Total Side

        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get system total overview",
            Description = "Returns aggregated system-level financial overview including total orders, invoices, balances and overall statistics.",
            Tags = ["Report - Total"]
        )]
        public async Task<TotalOverview> GetTotalOverview()
            => await _reportService.GetTotalOverviewAsync();


        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get all system orders",
            Description = "Returns paginated list of all orders across the system with filtering options.",
            Tags = ["Report - Total"]
        )]
        public async Task<OrderListResult> GetAllOrders(
            [FromBody] GetAllOrdersUpdate update)
            => await _reportService.GetAllOrdersAsync(update);


        [HttpPost("[action]")]
        [Authorize(Permissions.Reporter)]
        [CustomRateLimit(maxAttemptsCount: 30)]
        [SwaggerOperation(
            Summary = "Get all system invoices",
            Description = "Returns paginated list of all invoices across the system with filtering options.",
            Tags = ["Report - Total"]
        )]
        public async Task<InvoiceListResult> GetAllInvoices(
            [FromBody] GetAllInvoicesUpdate update)
            => await _reportService.GetAllInvoicesAsync(update);

        #endregion
    }



}
