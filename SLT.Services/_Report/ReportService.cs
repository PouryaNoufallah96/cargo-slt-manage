using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SLT.Domain.Collections;
using SLT.Domain.Repositories.Contracts;
using SLT.Services._Report.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utilities.Exceptions.Common;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Report
{
    public class ReportService(IInvoiceRepository _invoiceRepository, IOrderRepository _orderRepository) : IReportService, IScopedDependency
    {

        // hash base
        public async Task<OrderFullResult> GetSingleHashInvoiceDataAsync(HashUpdate update)
        {
            var invoice = await _invoiceRepository.AsQueryable()
                .Where(i => i.PaymentHash.ToLower() == update.Hash.ToLower()).FirstOrDefaultAsync() ?? throw new NotFoundException("invoice not found");

            var order = await _orderRepository.AsQueryable()
                .Where(o => (o.OrderId == invoice.OrderId)).FirstOrDefaultAsync() ?? throw new NotFoundException("Order not found!");

            var invoiceResult  = ConvertToInvoiceReslut(invoice);

            return ConvertToOrderFullReslut(new List<InvoiceResult> { invoiceResult }, order, null);
        }

        public async Task<OrderFullResult> GetOrderDetailAsync(OrderIdUpdate update)
        {
            var order = await _orderRepository.AsQueryable()
                 .Where(o => (o.OrderId.ToLower() == update.OrderOrTransferId.ToLower()
                           || o.TransferId.ToLower() == update.OrderOrTransferId.ToLower())
                         )
                .FirstOrDefaultAsync() ?? throw new NotFoundException("Order not found!");

            if (order == null)
                throw new BadRequestException("Order not found.");

            var invoices = await _invoiceRepository.AsQueryable()
                .Where(i => i.OrderId.ToLower() == order.OrderId.ToLower()).ToListAsync();
            
    
            var invoiceResults = invoices
                .Select(i => ConvertToInvoiceReslut(i, null))
                .ToList();

            return ConvertToOrderFullReslut(invoiceResults, order, null);
        }



        // wallet base 
        public async Task<OrderListResult> GetSingleWalletOrdersAsync(SingleWalletOrderListUpdate update)
        {
            if (string.IsNullOrWhiteSpace(update.Wallet))
                throw new BadRequestException("Wallet address is required.");
            string walletAddress = update.Wallet.ToLower(); 

            var query = _orderRepository.AsQueryable();


            if (update.ListType == OrderListType.Received)
            {
                query = query.Where(o => o.OwnerWallet.ToLower() != null && o.OwnerWallet.ToLower() == walletAddress);
            }
            else
            {
                query = query.Where(o => o.SeenBy.Contains(walletAddress));
            }


            if (update.States != null && update.States.Count != 0)
            {
                query = query.Where(o => update.States.Contains(o.State));
            }

            if(update.FromTime != null) 
            { query = query.Where(o => o.CreatedMoment >= update.FromTime); }

            if(update.ToTime != null)
            { query = query.Where(o => o.CreatedMoment <= update.ToTime); }
            

            var totalCount = await query.CountAsync();
            var pagination = update.Pagination;

            var pageCount = (int)Math.Ceiling(
                totalCount / (double)pagination.Size
            );

            var orders = await query
                .OrderByDescending(o => o.CreatedMoment)
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();

            var result = new OrderListResult
            {
                TotalCount = totalCount,
                PageCount = pageCount,
                Data = orders.Select(o => new OrderResult
                {
                    CreatedMoment = o.CreatedMoment,
                    ModifiedMoment = o.ModifiedMoment,
                    OrderId = o.OrderId,
                    TransferId = o.TransferId,

                    OwnerWallet = o.OwnerWallet,
                    PayerWallet = o.PayerWallet,
                    SeenBy = o.SeenBy,

                    TotalAmount = o.TotalAmount,
                    Transportation = o.Transportation,
                    Type = o.Type,
                    State = o.State,
                    PaymentDay = o.PaymentDay
                }).ToList()
            };

            return result;
        }

        public async Task<InvoiceListResult> GetSingleWalletInvoicesAsync(SingleWalletInvoiceListUpdate update)
        {
            if (string.IsNullOrWhiteSpace(update.Wallet))
                throw new BadRequestException("Wallet address is required.");
            string walletAddress = update.Wallet.ToLower();

            var query = _invoiceRepository.AsQueryable();


            if (update.ListType == OrderListType.Received)
            {
                query = query.Where(o => o.OwnerWallet.ToLower() != null && o.OwnerWallet.ToLower() == walletAddress);
            }
            else
            {
                query = query.Where(o => o.PayerWallet.ToLower() != null && o.PayerWallet.ToLower() == walletAddress);
            }


            if (update.States != null && update.States.Count != 0)
            {
                query = query.Where(o => update.States.Contains(o.State));
            }

            if (update.FromTime != null)
            { query = query.Where(o => o.CreatedMoment >= update.FromTime); }

            if (update.ToTime != null)
            { query = query.Where(o => o.CreatedMoment <= update.ToTime); }

            var totalCount = await query.CountAsync();
            var pagination = update.Pagination;

            var pageCount = (int)Math.Ceiling(
                totalCount / (double)pagination.Size
            );

            var invoices = await query
                .OrderByDescending(o => o.CreatedMoment)
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();

            var type = update.ListType == OrderListType.Received ? OwnershipType.Owner : OwnershipType.Payer;

            var result = new InvoiceListResult
            {
                TotalCount = totalCount,
                PageCount = pageCount,
                Data = invoices.Select(invoice => new InvoiceResult
                {
                    CreatedMoment = invoice.CreatedMoment,
                    ModifiedMoment = invoice.ModifiedMoment,
                    InvoiceId = invoice.InvoiceId,
                    OwnerWallet = invoice.OwnerWallet,
                    PayerWallet = invoice.PayerWallet,
                    OrderId = invoice.OrderId,
                    TokenSymbol = invoice.TokenSymbol,
                    TokenAddress = invoice.TokenAddress,
                    USDTAmount = invoice.USDTAmount,
                    USDTAmountInWei = invoice.USDTAmountInWei,
                    Desctiption = invoice.Desctiption,
                    TokenAmountAtPayment = invoice.TokenAmountAtPayment,
                    TokenAmountWeiAtPayment = invoice.TokenAmountWeiAtPayment,
                    TokenPriceAtPayment = invoice.TokenPriceAtPayment,
                    State = invoice.State,
                    PayMoment = invoice.PayMoment,
                    RegisterHash = invoice.RegisterHash,
                    PaymentHash = invoice.PaymentHash,
                    ActivateDate = invoice.ActivateDate,
                    OwnershipType = type
                }).ToList()
            };

            return result; 
        }

        public async Task<SingleWalletOverview> GetSingleWalletOverviewAsync(WalletUpdate update)
        {
            var ownerData = await GetOwnerOrderReportAsync(update.Wallet);
            var paterData = await GetPayerOrderReportAsync(update.Wallet);

            return new SingleWalletOverview
            {
                WalletAsOwnerReport = ownerData,
                WalletAsPayerReport = paterData
            };
        }

        private async Task<SingleWalletOverviewResult> GetOwnerOrderReportAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
                throw new ArgumentException("Wallet address is invalid.");

            var orders = await _orderRepository.AsQueryable()
                .Where(o => o.OwnerWallet.ToLower() == walletAddress.ToLower())
                .ToListAsync();

            var pendingOrders = orders.Count(o => o.State == OrderState.Pending);
            var doneOrders = orders.Count(o => o.State == OrderState.Completed);
            var totalOrders = orders.Count();

            var orderProgress = totalOrders == 0
                ? 0
                : Math.Round((decimal)doneOrders / totalOrders * 100, 2);

            var invoices = await _invoiceRepository.AsQueryable()
                .Where(i => i.OwnerWallet.ToLower() == walletAddress.ToLower())
                .ToListAsync();

            var totalInvoices = invoices.Count;
            var paidInvoices = invoices.Count(i => i.State == InvoiceState.Completed);
            var pendingInvoices = invoices.Count(i => i.State == InvoiceState.Pending);

            var invoiceProgress = totalInvoices == 0
                ? 0
                : Math.Round((decimal)paidInvoices / totalInvoices * 100, 2);

            return new SingleWalletOverviewResult
            {
                TotalOrderCount = totalOrders,
                PendingOrderCount = pendingOrders,
                DoneOrderCount = doneOrders,
                OrderProgress = orderProgress,

                TotalInvoiceCount = totalInvoices,
                PaidInvoiceCount = paidInvoices,
                PendingInvoiceCount = pendingInvoices,
                InvoiceProgress = invoiceProgress
            };
        }

        private async Task<SingleWalletOverviewResult> GetPayerOrderReportAsync(string walletAddress)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
                throw new ArgumentException("Wallet address is invalid.");

            var orders = await _orderRepository.AsQueryable()
                .Where(o => o.SeenBy.Contains(walletAddress.ToLower()))
                .ToListAsync();

            var orderids = orders.Select(o => o.OrderId);

            var pendingOrders = orders.Count(o => o.State == OrderState.Pending);
            var doneOrders = orders.Count(o => o.State == OrderState.Completed);
            var totalOrders = orders.Count();

            var orderProgress = totalOrders == 0
                ? 0
                : Math.Round((decimal)doneOrders / totalOrders * 100, 2);

            var invoices = await _invoiceRepository.AsQueryable()
                .Where(i => orderids.Contains(i.OrderId))
                .ToListAsync();

            var totalInvoices = invoices.Count;
            var paidInvoices = invoices.Count(i => i.State == InvoiceState.Completed && i.PayerWallet.ToLower() == walletAddress.ToLower());
            var pendingInvoices = invoices.Count(i => i.State == InvoiceState.Pending);

            var invoiceProgress = totalInvoices == 0
                ? 0
                : Math.Round((decimal)paidInvoices / totalInvoices * 100, 2);

            return new SingleWalletOverviewResult
            {
                TotalOrderCount = orderids.Count(),
                PendingOrderCount = pendingOrders,
                DoneOrderCount = doneOrders,
                OrderProgress = orderProgress,

                TotalInvoiceCount = totalInvoices,
                PaidInvoiceCount = paidInvoices,
                PendingInvoiceCount = pendingInvoices,
                InvoiceProgress = invoiceProgress
            };
        }


        // total side
        public async Task<TotalOverview> GetTotalOverviewAsync()
        {

            var orders = await _orderRepository.AsQueryable()
                .ToListAsync();

            var totalOrders = orders.Count();
            var totalOrderAmountInUsdt = orders.Sum(o => o.TotalAmount);
            var pendingOrders = orders.Count(o => o.State == OrderState.Pending);
            var pendingOrderAmountInUsdt = orders.Where(o => o.State == OrderState.Pending).Sum(o => o.TotalAmount);
            var doneOrders = orders.Count(o => o.State == OrderState.Completed);
            var doneOrderAmountInUsdt = orders.Where(o => o.State == OrderState.Completed).Sum(o => o.TotalAmount);

            var orderProgress = totalOrders == 0
                ? 0
                : Math.Round((decimal)doneOrders / totalOrders * 100, 2);

            var invoices = await _invoiceRepository.AsQueryable()
                .ToListAsync();

            var totalInvoices = invoices.Count;
            var totalInvoiceAmountInUsdt = invoices.Sum(i => i.USDTAmount);
            var paidInvoices = invoices.Count(i => i.State == InvoiceState.Completed);
            var paidInvoiceAmountInUsdt = invoices.Where(i => i.State == InvoiceState.Completed).Sum(i => i.USDTAmount);
            var pendingInvoices = invoices.Count(i => i.State == InvoiceState.Pending);
            var pendingInvoiceAmountInUsdt = invoices.Where(i => i.State == InvoiceState.Pending).Sum(i => i.USDTAmount);

            var invoiceProgress = totalInvoices == 0
                ? 0
                : Math.Round((decimal)paidInvoices / totalInvoices * 100, 2);

            return new TotalOverview
            {
                TotalOrderCount = totalOrders,
                PendingOrderCount = pendingOrders,
                DoneOrderCount = doneOrders,
                OrderProgress = orderProgress,
                DoneOrderAmountInUsdt = doneOrderAmountInUsdt,
                PendingOrderAmountInUsdt = pendingOrderAmountInUsdt, 
                TotalOrderAmountInUsdt = totalOrderAmountInUsdt,

                TotalInvoiceCount = totalInvoices,
                PaidInvoiceCount = paidInvoices,
                PendingInvoiceCount = pendingInvoices,
                InvoiceProgress = invoiceProgress,
                PaidInvoiceAmountInUsdt = paidInvoiceAmountInUsdt,
                PendingInvoiceAmountInUsdt = pendingInvoiceAmountInUsdt,
                TotalInvoiceAmountInUsdt = totalInvoiceAmountInUsdt
            };
        }

        public async Task<OrderListResult> GetAllOrdersAsync(GetAllOrdersUpdate update)
        {

          
            var query = _orderRepository.AsQueryable();



            if (update.States != null && update.States.Count != 0)
            {
                query = query.Where(o => update.States.Contains(o.State));
            }

            if (update.FromTime != null)
            { query = query.Where(o => o.CreatedMoment >= update.FromTime); }

            if (update.ToTime != null)
            { query = query.Where(o => o.CreatedMoment <= update.ToTime); }


            var totalCount = await query.CountAsync();
            var pagination = update.Pagination;

            var pageCount = (int)Math.Ceiling(
                totalCount / (double)pagination.Size
            );

            var orders = await query
                .OrderByDescending(o => o.CreatedMoment)
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();

            var result = new OrderListResult
            {
                TotalCount = totalCount,
                PageCount = pageCount,
                Data = orders.Select(o => new OrderResult
                {
                    CreatedMoment = o.CreatedMoment,
                    ModifiedMoment = o.ModifiedMoment,
                    OrderId = o.OrderId,
                    TransferId = o.TransferId,

                    OwnerWallet = o.OwnerWallet,
                    PayerWallet = o.PayerWallet,
                    SeenBy = o.SeenBy,

                    TotalAmount = o.TotalAmount,
                    Transportation = o.Transportation,
                    Type = o.Type,
                    State = o.State,
                    PaymentDay = o.PaymentDay
                }).ToList()
            };

            return result;
        }

        public async Task<InvoiceListResult> GetAllInvoicesAsync(GetAllInvoicesUpdate update)
        {
          
            var query = _invoiceRepository.AsQueryable();


            if (update.States != null && update.States.Count != 0)
            {
                query = query.Where(o => update.States.Contains(o.State));
            }

            if (update.FromTime != null)
            { query = query.Where(o => o.CreatedMoment >= update.FromTime); }

            if (update.ToTime != null)
            { query = query.Where(o => o.CreatedMoment <= update.ToTime); }

            var totalCount = await query.CountAsync();
            var pagination = update.Pagination;

            var pageCount = (int)Math.Ceiling(
                totalCount / (double)pagination.Size
            );

            var invoices = await query
                .OrderByDescending(o => o.CreatedMoment)
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();


            var result = new InvoiceListResult
            {
                TotalCount = totalCount,
                PageCount = pageCount,
                Data = invoices.Select(invoice => new InvoiceResult
                {
                    CreatedMoment = invoice.CreatedMoment,
                    ModifiedMoment = invoice.ModifiedMoment,
                    InvoiceId = invoice.InvoiceId,
                    OwnerWallet = invoice.OwnerWallet,
                    PayerWallet = invoice.PayerWallet,
                    OrderId = invoice.OrderId,
                    TokenSymbol = invoice.TokenSymbol,
                    TokenAddress = invoice.TokenAddress,
                    USDTAmount = invoice.USDTAmount,
                    USDTAmountInWei = invoice.USDTAmountInWei,
                    Desctiption = invoice.Desctiption,
                    TokenAmountAtPayment = invoice.TokenAmountAtPayment,
                    TokenAmountWeiAtPayment = invoice.TokenAmountWeiAtPayment,
                    TokenPriceAtPayment = invoice.TokenPriceAtPayment,
                    State = invoice.State,
                    PayMoment = invoice.PayMoment,
                    RegisterHash = invoice.RegisterHash,
                    PaymentHash = invoice.PaymentHash,
                    ActivateDate = invoice.ActivateDate,
                    OwnershipType = null
                }).ToList()
            };

            return result;
        }




        /// <summary>
        /// use for convert to result
        /// </summary>
        /// <param name="invoice"></param>
        /// <returns></returns>
        private InvoiceResult ConvertToInvoiceReslut(Invoice invoice, OwnershipType? type = null)
        {
            return new InvoiceResult
            {
                CreatedMoment = invoice.CreatedMoment,
                ModifiedMoment = invoice.ModifiedMoment,
                InvoiceId = invoice.InvoiceId,
                OwnerWallet = invoice.OwnerWallet,
                PayerWallet = invoice.PayerWallet,
                OrderId = invoice.OrderId,
                TokenSymbol = invoice.TokenSymbol,
                TokenAddress = invoice.TokenAddress,
                USDTAmount = invoice.USDTAmount,
                USDTAmountInWei = invoice.USDTAmountInWei,
                Desctiption = invoice.Desctiption,
                TokenAmountAtPayment = invoice.TokenAmountAtPayment,
                TokenAmountWeiAtPayment = invoice.TokenAmountWeiAtPayment,
                TokenPriceAtPayment = invoice.TokenPriceAtPayment,
                State = invoice.State,
                PayMoment = invoice.PayMoment,
                RegisterHash = invoice.RegisterHash,
                PaymentHash = invoice.PaymentHash,
                ActivateDate = invoice.ActivateDate,
                OwnershipType = type
            };

        }


        /// <summary>
        /// use for convert to result
        /// </summary>
        /// <param name="invoiceResults"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        private OrderFullResult ConvertToOrderFullReslut(List<InvoiceResult> invoiceResults, Order order, OwnershipType? type = null)
        {
            return new OrderFullResult
            {
                CreatedMoment = order.CreatedMoment,
                ModifiedMoment = order.ModifiedMoment,
                OrderId = order.OrderId,
                OwnerWallet = order.OwnerWallet,
                PayerWallet = order.PayerWallet,
                SeenBy = order.SeenBy,
                TotalAmount = order.TotalAmount,
                Transportation = order.Transportation,
                Type = order.Type,
                State = order.State,
                PaymentDay = order.PaymentDay,
                TransferId = order.TransferId,
                Invoices = invoiceResults,
                OwnershipType = type
            };
        }

       
    }
}
