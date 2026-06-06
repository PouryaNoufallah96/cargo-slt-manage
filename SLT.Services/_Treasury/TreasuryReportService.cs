using MongoDB.Driver;
using MongoDB.Driver.Linq;
using SLT.Domain.Collections;
using SLT.Domain.Repositories.Contracts;
using SLT.Services._Treasury.Calculators;
using SLT.Services._Treasury.DTOs.Results;
using SLT.Services._Treasury.DTOs.Updates;
using Utilities.DTOs;
using Utilities.Exceptions.Common;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._Treasury
{
    public class TreasuryReportService(
        IStakeRepository _stakeRepository,
        IWithdrawalRepository _withdrawalRepository,
        IStakeReportingStatusResolver _statusResolver,
        IObligationCalculator _obligationCalculator,
        IPeriodBucketer _periodBucketer,
        IDepositTimeValuator _depositTimeValuator,
        ISummaryTextBuilder _summaryTextBuilder) : ITreasuryReportService, IScopedDependency
    {
        public async Task<TreasuryOverviewResult> GetOverviewAsync()
        {
            var reportAsOfMoment = DateTime.UtcNow;

            // LUSD/GOLDGR only; NotRegistered excluded.
            var stakes = await _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol))
                .ToListAsync();

            var warnings = new List<string>();

            var openStakes = new List<OpenStake>();
            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                if (status == TreasuryReportingStatus.Finished)
                    continue;

                var obligation = _obligationCalculator.Calculate(stake);
                if (obligation.ProfitAnomaly)
                {
                    warnings.Add(
                        $"Stake {stake.StakeReference} ({stake.TokenSymbol}): paid profit exceeds expected full-term profit "
                        + $"(expected {obligation.ExpectedFullTermProfit}, paid {obligation.PaidProfitTotal}); remaining profit clamped to 0.");
                }

                openStakes.Add(new OpenStake(stake, status, obligation));
            }

            var perAsset = TreasuryAssets.Universe
                .Select(asset =>
                {
                    var symbol = TreasuryAssets.SymbolOf(asset);
                    var assetStakes = openStakes
                        .Where(o => string.Equals(o.Stake.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    var openPrincipal = assetStakes.Sum(o => o.Obligation.RemainingPrincipalOwed);
                    var remainingProfitOwed = assetStakes.Sum(o => o.Obligation.RemainingProfitOwed);

                    return new TreasuryAssetObligationSummary
                    {
                        Asset = symbol,
                        OpenPrincipal = openPrincipal,
                        RemainingProfitOwed = remainingProfitOwed,
                        OpenObligation = openPrincipal + remainingProfitOwed
                    };
                })
                .ToList();

            var withinTerm = openStakes.Count(o => o.Status == TreasuryReportingStatus.WithinTerm);
            var maturedUnredeemed = openStakes.Count(o => o.Status == TreasuryReportingStatus.MaturedUnredeemed);

            var activeContracts = new TreasuryActiveContractsSummary
            {
                WithinTerm = withinTerm,
                MaturedUnredeemed = maturedUnredeemed,
                Total = withinTerm + maturedUnredeemed
            };

            var activeUsers = openStakes
                .Select(o => (o.Stake.WalletAddress ?? string.Empty).ToLower())
                .Where(w => !string.IsNullOrEmpty(w))
                .Distinct()
                .Count();

            var nearestMaturities = openStakes
                .OrderBy(o => o.Stake.EndMoment)
                .Take(5)
                .Select(o => new TreasuryUpcomingMaturity
                {
                    StakeReference = o.Stake.StakeReference,
                    Asset = o.Stake.TokenSymbol,
                    WalletAddress = o.Stake.WalletAddress,
                    EndMoment = o.Stake.EndMoment,
                    RemainingPrincipal = o.Obligation.RemainingPrincipalOwed
                })
                .ToList();

            var result = new TreasuryOverviewResult
            {
                ReportAsOfMoment = reportAsOfMoment,
                PerAsset = perAsset,
                ActiveContracts = activeContracts,
                ActiveUsers = activeUsers,
                NearestMaturities = nearestMaturities,
                Warnings = warnings
            };

            result.SummaryText = _summaryTextBuilder.BuildOverviewSummary(result);

            return result;
        }

        public async Task<PlanTypeResult> GetPlanTypesAsync()
        {
            // LUSD/GOLDGR only; NotRegistered excluded (Finished included).
            var durations = await _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol))
                .Select(s => s.MonthDuration)
                .ToListAsync();

            return new PlanTypeResult
            {
                PlanTypes = durations
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => new PlanTypeOption { Key = d, Label = $"{d} month plan" })
                    .ToList()
            };
        }

        public async Task<MaturityCalendarResult> GetMaturityCalendarAsync(MaturityCalendarUpdate update)
        {
            update ??= new MaturityCalendarUpdate();

            var reportAsOfMoment = DateTime.UtcNow;

            var window = _periodBucketer.ResolveMaturityWindow(
                update.Horizon, update.CustomFrom, update.CustomTo, reportAsOfMoment);

            var query = _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol));

            if (update.Asset != TreasuryAssetFilter.All)
            {
                var assetSymbol = update.Asset == TreasuryAssetFilter.GOLDGR
                    ? TreasuryAssets.Goldgr
                    : TreasuryAssets.Lusd;
                query = query.Where(s => s.TokenSymbol == assetSymbol);
            }

            if (update.PlanType.HasValue)
            {
                var plan = update.PlanType.Value;
                query = query.Where(s => s.MonthDuration == plan);
            }

            var stakes = await query.ToListAsync();

            var warnings = new List<string>();

            var dueNowOverdue = new List<OpenStake>();
            var futureMaturing = new List<OpenStake>();

            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                if (status == TreasuryReportingStatus.Finished)
                    continue;

                var obligation = _obligationCalculator.Calculate(stake);
                if (obligation.ProfitAnomaly)
                {
                    warnings.Add(
                        $"Stake {stake.StakeReference} ({stake.TokenSymbol}): paid profit exceeds expected full-term profit "
                        + $"(expected {obligation.ExpectedFullTermProfit}, paid {obligation.PaidProfitTotal}); remaining profit clamped to 0.");
                }

                var openStake = new OpenStake(stake, status, obligation);

                // Overdue is always shown separately — not folded into the horizon window.
                if (status == TreasuryReportingStatus.MaturedUnredeemed)
                {
                    dueNowOverdue.Add(openStake);
                }
                else if (stake.EndMoment >= window.FromInclusive && stake.EndMoment < window.ToExclusive)
                {
                    futureMaturing.Add(openStake);
                }
            }

            var result = new MaturityCalendarResult
            {
                ReportAsOfMoment = reportAsOfMoment,
                FromInclusive = window.FromInclusive,
                ToExclusive = window.ToExclusive,
                DueNowOverdue = BuildBucket(dueNowOverdue),
                FutureMaturing = BuildBucket(futureMaturing),
                Warnings = warnings
            };

            result.SummaryText = _summaryTextBuilder.BuildMaturityCalendarSummary(result);

            return result;
        }

        public async Task<ActiveContractsResult> GetActiveContractsAsync(ActiveContractsUpdate update)
        {
            update ??= new ActiveContractsUpdate();
            var pagination = update.Pagination ?? new Pagination();

            var reportAsOfMoment = DateTime.UtcNow;

            var query = _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol));

            // Status filter runs in Mongo so CountAsync/paging match the filtered set.
            switch (update.Status)
            {
                case ActiveContractsStatusFilter.Open:
                    query = query.Where(s => s.State == StakeState.Active);
                    break;
                case ActiveContractsStatusFilter.WithinTerm:
                    query = query.Where(s => s.State == StakeState.Active && s.EndMoment > reportAsOfMoment);
                    break;
                case ActiveContractsStatusFilter.MaturedUnredeemed:
                    query = query.Where(s => s.State == StakeState.Active && s.EndMoment <= reportAsOfMoment);
                    break;
                case ActiveContractsStatusFilter.Finished:
                    query = query.Where(s => s.State == StakeState.Finished);
                    break;
                case ActiveContractsStatusFilter.All:
                    break;
            }

            if (update.Asset != TreasuryAssetFilter.All)
            {
                var assetSymbol = update.Asset == TreasuryAssetFilter.GOLDGR
                    ? TreasuryAssets.Goldgr
                    : TreasuryAssets.Lusd;
                query = query.Where(s => s.TokenSymbol == assetSymbol);
            }

            if (update.PlanType.HasValue)
            {
                var plan = update.PlanType.Value;
                query = query.Where(s => s.MonthDuration == plan);
            }

            if (!string.IsNullOrEmpty(update.Wallet))
            {
                var wallet = update.Wallet.ToLower();
                query = query.Where(s => s.WalletAddress.ToLower() == wallet);
            }

            if (!string.IsNullOrEmpty(update.ContractId))
            {
                var contractId = update.ContractId.ToLower();
                query = query.Where(s => s.StakeReference.ToLower() == contractId);
            }

            if (update.StartFrom.HasValue)
            {
                var startFrom = update.StartFrom.Value;
                query = query.Where(s => s.StartMoment >= startFrom);
            }
            if (update.StartTo.HasValue)
            {
                var startTo = update.StartTo.Value;
                query = query.Where(s => s.StartMoment < startTo);
            }

            if (update.EndFrom.HasValue)
            {
                var endFrom = update.EndFrom.Value;
                query = query.Where(s => s.EndMoment >= endFrom);
            }
            if (update.EndTo.HasValue)
            {
                var endTo = update.EndTo.Value;
                query = query.Where(s => s.EndMoment < endTo);
            }

            var totalCount = await query.CountAsync();
            var pageCount = (int)Math.Ceiling(totalCount / (double)pagination.Size);

            // StakeReference tiebreaker keeps page boundaries stable when EndMoment ties.
            var stakes = await query
                .OrderBy(s => s.EndMoment)
                .ThenBy(s => s.StakeReference)
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToListAsync();

            var warnings = new List<string>();

            var rows = new List<ActiveContractRow>();
            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                var obligation = _obligationCalculator.Calculate(stake);

                if (obligation.ProfitAnomaly)
                {
                    warnings.Add(
                        $"Stake {stake.StakeReference} ({stake.TokenSymbol}): paid profit exceeds expected full-term profit "
                        + $"(expected {obligation.ExpectedFullTermProfit}, paid {obligation.PaidProfitTotal}); remaining profit clamped to 0.");
                }

                rows.Add(new ActiveContractRow
                {
                    WalletAddress = stake.WalletAddress,
                    StakeReference = stake.StakeReference,
                    Asset = stake.TokenSymbol,
                    Principal = obligation.RemainingPrincipalOwed,
                    PlanType = stake.MonthDuration,
                    StartMoment = stake.StartMoment,
                    EndMoment = stake.EndMoment,
                    ProfitReceived = obligation.PaidProfitTotal,
                    RemainingProfitOwed = obligation.RemainingProfitOwed,
                    Status = status
                });
            }

            var result = new ActiveContractsResult
            {
                Data = rows,
                TotalCount = totalCount,
                PageCount = pageCount,
                Warnings = warnings
            };

            result.SummaryText = _summaryTextBuilder.BuildActiveContractsSummary(result);

            return result;
        }

        public async Task<WalletAnalysisResult> GetWalletAnalysisAsync(WalletAnalysisUpdate update)
        {
            update ??= new WalletAnalysisUpdate();

            if (string.IsNullOrEmpty(update.Wallet))
                throw new BadRequestException("Wallet is required.");

            var reportAsOfMoment = DateTime.UtcNow;

            var wallet = update.Wallet.ToLower();

            // Unlike dashboard/calendar, Finished stakes stay in — they count toward entered totals.
            var stakes = await _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol)
                         && s.WalletAddress.ToLower() == wallet)
                .ToListAsync();

            var warnings = new List<string>();

            var withinTerm = 0;
            var maturedUnredeemed = 0;
            var finished = 0;
            var openStakes = new List<OpenStake>();

            var enteredPrincipal = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var profitReceived = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var assetContractCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                var obligation = _obligationCalculator.Calculate(stake);

                var symbol = stake.TokenSymbol ?? string.Empty;
                enteredPrincipal[symbol] = enteredPrincipal.GetValueOrDefault(symbol) + stake.StartAmount;
                profitReceived[symbol] = profitReceived.GetValueOrDefault(symbol) + obligation.PaidProfitTotal;
                assetContractCount[symbol] = assetContractCount.GetValueOrDefault(symbol) + 1;

                switch (status)
                {
                    case TreasuryReportingStatus.WithinTerm:
                        withinTerm++;
                        openStakes.Add(new OpenStake(stake, status, obligation));
                        break;
                    case TreasuryReportingStatus.MaturedUnredeemed:
                        maturedUnredeemed++;
                        openStakes.Add(new OpenStake(stake, status, obligation));
                        break;
                    case TreasuryReportingStatus.Finished:
                        finished++;
                        break;
                }

                if (status != TreasuryReportingStatus.Finished && obligation.ProfitAnomaly)
                {
                    warnings.Add(
                        $"Stake {stake.StakeReference} ({stake.TokenSymbol}): paid profit exceeds expected full-term profit "
                        + $"(expected {obligation.ExpectedFullTermProfit}, paid {obligation.PaidProfitTotal}); remaining profit clamped to 0.");
                }
            }

            var enteredByAsset = TreasuryAssets.Universe
                .Select(asset =>
                {
                    var symbol = TreasuryAssets.SymbolOf(asset);
                    return new WalletAssetEntered
                    {
                        Asset = symbol,
                        ContractCount = assetContractCount.GetValueOrDefault(symbol),
                        TotalEnteredPrincipal = enteredPrincipal.GetValueOrDefault(symbol),
                        TotalProfitReceived = profitReceived.GetValueOrDefault(symbol)
                    };
                })
                .ToList();

            var nearest = openStakes
                .OrderBy(o => o.Stake.EndMoment)
                .Select(o => new WalletUpcomingMaturity
                {
                    StakeReference = o.Stake.StakeReference,
                    Asset = o.Stake.TokenSymbol,
                    EndMoment = o.Stake.EndMoment,
                    RemainingPrincipal = o.Obligation.RemainingPrincipalOwed
                })
                .FirstOrDefault();

            var result = new WalletAnalysisResult
            {
                Wallet = update.Wallet,
                ReportAsOfMoment = reportAsOfMoment,
                RegisteredContractCount = stakes.Count,
                EnteredByAsset = enteredByAsset,
                WithinTermCount = withinTerm,
                MaturedUnredeemedCount = maturedUnredeemed,
                ActiveOpenCount = withinTerm + maturedUnredeemed,
                FinishedCount = finished,
                NearestMaturity = nearest,
                Warnings = warnings
            };

            result.Ranking = await ComputeWalletRankingAsync(wallet, update.TopRankThreshold, warnings);

            result.SummaryText = _summaryTextBuilder.BuildWalletAnalysisSummary(result);

            return result;
        }

        public async Task<ContractDistributionResult> GetContractDistributionAsync(ContractDistributionUpdate update)
        {
            update ??= new ContractDistributionUpdate();

            var reportAsOfMoment = DateTime.UtcNow;

            var query = _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol));

            if (update.Asset != TreasuryAssetFilter.All)
            {
                var assetSymbol = update.Asset == TreasuryAssetFilter.GOLDGR
                    ? TreasuryAssets.Goldgr
                    : TreasuryAssets.Lusd;
                query = query.Where(s => s.TokenSymbol == assetSymbol);
            }

            if (update.PlanType.HasValue)
            {
                var plan = update.PlanType.Value;
                query = query.Where(s => s.MonthDuration == plan);
            }

            var stakes = await query.ToListAsync();

            var warnings = new List<string>();

            var openStakes = new List<OpenStake>();
            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                if (status == TreasuryReportingStatus.Finished)
                    continue;

                var obligation = _obligationCalculator.Calculate(stake);
                openStakes.Add(new OpenStake(stake, status, obligation));
            }

            var planUsd = new Dictionary<int, decimal>();
            var totalUsd = 0m;
            var invalidPriceCount = 0;

            // Value-weighted % uses remaining open principal (TokenAmount), not StartAmount.
            foreach (var open in openStakes)
            {
                var basis = open.Obligation.RemainingPrincipalOwed;
                if (_depositTimeValuator.TryValueUsd(basis, open.Stake.TokenPrice, out var valueUsd))
                {
                    planUsd[open.Stake.MonthDuration] = planUsd.GetValueOrDefault(open.Stake.MonthDuration) + valueUsd;
                    totalUsd += valueUsd;
                }
                else
                {
                    invalidPriceCount++;
                }
            }

            if (invalidPriceCount > 0)
            {
                warnings.Add(
                    $"{invalidPriceCount} stake{(invalidPriceCount == 1 ? "" : "s")} had a missing/zero deposit-time price; "
                    + "value-weighted % is incomplete; native per-asset values are unaffected.");
            }

            var plans = openStakes
                .Select(o => o.Stake.MonthDuration)
                .Distinct()
                .OrderBy(d => d)
                .Select(duration =>
                {
                    var planStakes = openStakes
                        .Where(o => o.Stake.MonthDuration == duration)
                        .ToList();

                    var byAsset = TreasuryAssets.Universe
                        .Select(asset =>
                        {
                            var symbol = TreasuryAssets.SymbolOf(asset);
                            var openPrincipal = planStakes
                                .Where(o => string.Equals(o.Stake.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                                .Sum(o => o.Obligation.RemainingPrincipalOwed);

                            return new PlanAssetCapital
                            {
                                Asset = symbol,
                                OpenPrincipal = openPrincipal
                            };
                        })
                        .ToList();

                    var share = totalUsd == 0m
                        ? 0m
                        : planUsd.GetValueOrDefault(duration) / totalUsd * 100m;

                    return new PlanDistribution
                    {
                        PlanType = duration,
                        ContractCount = planStakes.Count,
                        ByAsset = byAsset,
                        ValueWeightedSharePercent = share
                    };
                })
                .ToList();

            var result = new ContractDistributionResult
            {
                ReportAsOfMoment = reportAsOfMoment,
                ValuationMethod = ValuationMethod.DepositTimeTokenPrice,
                ValuationNote = _depositTimeValuator.ValuationLabel,
                Plans = plans,
                ValuationComplete = invalidPriceCount == 0,
                Warnings = warnings
            };

            result.SummaryText = _summaryTextBuilder.BuildContractDistributionSummary(result);

            return result;
        }

        public async Task<HistoricalPerformanceResult> GetHistoricalPerformanceAsync(HistoricalPerformanceUpdate update)
        {
            update ??= new HistoricalPerformanceUpdate();
            var pagination = update.Pagination ?? new Pagination();

            if (update.From >= update.To)
                throw new BadRequestException("From must be strictly earlier than To.");

            var reportAsOfMoment = DateTime.UtcNow;

            var warnings = new List<string>();

            var periods = _periodBucketer.ResolveCalendarPeriods(update.Grouping, update.From, update.To);

            var from = update.From;
            var to = update.To;

            // Inflows: new contracts + attracted capital, anchored on StartMoment.
            var stakeQuery = _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol)
                         && s.StartMoment >= from
                         && s.StartMoment < to);

            if (update.Asset != TreasuryAssetFilter.All)
            {
                var assetSymbol = update.Asset == TreasuryAssetFilter.GOLDGR
                    ? TreasuryAssets.Goldgr
                    : TreasuryAssets.Lusd;
                stakeQuery = stakeQuery.Where(s => s.TokenSymbol == assetSymbol);
            }

            var inflowStakes = await stakeQuery
                .Select(s => new { s.StartMoment, s.TokenSymbol, s.StartAmount })
                .ToListAsync();

            // Payouts + completions from Withdrawal ledger (Success), anchored on CreatedMoment.
            var withdrawalQuery = _withdrawalRepository.AsQueryable()
                .Where(w => w.State == WithdrawalState.Success
                         && TreasuryAssets.UniverseSymbols.Contains(w.Symbol)
                         && w.CreatedMoment >= from
                         && w.CreatedMoment < to);

            if (update.Asset != TreasuryAssetFilter.All)
            {
                var assetSymbol = update.Asset == TreasuryAssetFilter.GOLDGR
                    ? TreasuryAssets.Goldgr
                    : TreasuryAssets.Lusd;
                withdrawalQuery = withdrawalQuery.Where(w => w.Symbol == assetSymbol);
            }

            var successWithdrawals = await withdrawalQuery
                .Select(w => new { w.CreatedMoment, w.Symbol, w.ProfitAmount, w.Type, w.StakeReference })
                .ToListAsync();

            var allPeriods = new List<PeriodPerformance>();
            foreach (var period in periods)
            {
                var periodStakes = inflowStakes
                    .Where(s => s.StartMoment >= period.FromInclusive && s.StartMoment < period.ToExclusive)
                    .ToList();

                var periodWithdrawals = successWithdrawals
                    .Where(w => w.CreatedMoment >= period.FromInclusive && w.CreatedMoment < period.ToExclusive)
                    .ToList();

                var attractedByAsset = TreasuryAssets.Universe
                    .Select(asset =>
                    {
                        var symbol = TreasuryAssets.SymbolOf(asset);
                        return new PeriodAssetAmount
                        {
                            Asset = symbol,
                            Amount = periodStakes
                                .Where(s => string.Equals(s.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                                .Sum(s => s.StartAmount)
                        };
                    })
                    .ToList();

                var profitPaidByAsset = TreasuryAssets.Universe
                    .Select(asset =>
                    {
                        var symbol = TreasuryAssets.SymbolOf(asset);
                        return new PeriodAssetAmount
                        {
                            Asset = symbol,
                            Amount = periodWithdrawals
                                .Where(w => string.Equals(w.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
                                .Sum(w => w.ProfitAmount)
                        };
                    })
                    .ToList();

                var finishedContractCount = periodWithdrawals
                    .Where(w => w.Type == WithdrawalType.StakeWithdrawal)
                    .Select(w => w.StakeReference)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                allPeriods.Add(new PeriodPerformance
                {
                    Label = period.Label,
                    FromInclusive = period.FromInclusive,
                    ToExclusive = period.ToExclusive,
                    NewContractCount = periodStakes.Count,
                    AttractedByAsset = attractedByAsset,
                    ProfitPaidByAsset = profitPaidByAsset,
                    FinishedContractCount = finishedContractCount
                });
            }

            if (successWithdrawals.Count == 0 && inflowStakes.Count > 0)
            {
                warnings.Add(
                    "No Success withdrawals were found in the requested range while contract inflows exist; "
                    + "the withdrawal ledger may be incomplete for this period. Profit-paid and finished-contract "
                    + "figures reflect only available ledger data and are not approximated.");
            }

            var totalCount = allPeriods.Count;
            var pageCount = (int)Math.Ceiling(totalCount / (double)pagination.Size);

            var result = new HistoricalPerformanceResult
            {
                ReportAsOfMoment = reportAsOfMoment,
                Data = allPeriods,
                TotalCount = totalCount,
                PageCount = pageCount,
                Warnings = warnings
            };

            // Summary needs the full period set — build before paging.
            result.SummaryText = _summaryTextBuilder.BuildHistoricalPerformanceSummary(result);

            result.Data = allPeriods
                .Skip((pagination.Page - 1) * pagination.Size)
                .Take(pagination.Size)
                .ToList();

            return result;
        }

        public async Task<FutureObligationsResult> GetFutureObligationsAsync(FutureObligationsUpdate update)
        {
            update ??= new FutureObligationsUpdate();

            var reportAsOfMoment = DateTime.UtcNow;

            var window = _periodBucketer.ResolveMaturityWindow(
                update.Horizon, update.CustomFrom, update.CustomTo, reportAsOfMoment);

            var query = _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol));

            if (update.Asset != TreasuryAssetFilter.All)
            {
                var assetSymbol = update.Asset == TreasuryAssetFilter.GOLDGR
                    ? TreasuryAssets.Goldgr
                    : TreasuryAssets.Lusd;
                query = query.Where(s => s.TokenSymbol == assetSymbol);
            }

            if (update.PlanType.HasValue)
            {
                var plan = update.PlanType.Value;
                query = query.Where(s => s.MonthDuration == plan);
            }

            var stakes = await query.ToListAsync();

            var warnings = new List<string>();

            var dueNowOverdue = new List<OpenStake>();
            var futureWithinHorizon = new List<OpenStake>();

            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                if (status == TreasuryReportingStatus.Finished)
                    continue;

                var obligation = _obligationCalculator.Calculate(stake);
                if (obligation.ProfitAnomaly)
                {
                    warnings.Add(
                        $"Stake {stake.StakeReference} ({stake.TokenSymbol}): paid profit exceeds expected full-term profit "
                        + $"(expected {obligation.ExpectedFullTermProfit}, paid {obligation.PaidProfitTotal}); remaining profit clamped to 0.");
                }

                var openStake = new OpenStake(stake, status, obligation);

                if (status == TreasuryReportingStatus.MaturedUnredeemed)
                {
                    dueNowOverdue.Add(openStake);
                }
                else if (stake.EndMoment >= window.FromInclusive && stake.EndMoment < window.ToExclusive)
                {
                    futureWithinHorizon.Add(openStake);
                }
                // Within-term but maturing after the window: not counted in either bucket.
            }

            var dueByAsset = BuildAssetObligations(dueNowOverdue);
            var futureByAsset = BuildAssetObligations(futureWithinHorizon);

            var totalByAsset = TreasuryAssets.Universe
                .Select(asset =>
                {
                    var symbol = TreasuryAssets.SymbolOf(asset);
                    var due = dueByAsset.First(a => a.Asset == symbol);
                    var future = futureByAsset.First(a => a.Asset == symbol);

                    return new AssetObligation
                    {
                        Asset = symbol,
                        PrincipalObligation = due.PrincipalObligation + future.PrincipalObligation,
                        ProfitObligation = due.ProfitObligation + future.ProfitObligation,
                        TotalObligation = due.TotalObligation + future.TotalObligation
                    };
                })
                .ToList();

            var result = new FutureObligationsResult
            {
                ReportAsOfMoment = reportAsOfMoment,
                FromInclusive = window.FromInclusive,
                ToExclusive = window.ToExclusive,
                DueNowOverdue = dueByAsset,
                FutureWithinHorizon = futureByAsset,
                TotalRequired = totalByAsset,
                Warnings = warnings
            };

            result.SummaryText = _summaryTextBuilder.BuildFutureObligationsSummary(result);

            return result;
        }

        public async Task<AssetCompositionResult> GetAssetCompositionAsync(AssetCompositionUpdate update)
        {
            update ??= new AssetCompositionUpdate();

            var reportAsOfMoment = DateTime.UtcNow;

            var query = _stakeRepository.AsQueryable()
                .Where(s => s.State != StakeState.NotRegistered
                         && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol));

            if (update.PlanType.HasValue)
            {
                var plan = update.PlanType.Value;
                query = query.Where(s => s.MonthDuration == plan);
            }

            var stakes = await query.ToListAsync();

            var warnings = new List<string>();

            var openStakes = new List<OpenStake>();
            foreach (var stake in stakes)
            {
                var status = _statusResolver.Resolve(stake.State, stake.EndMoment, reportAsOfMoment);
                if (status == TreasuryReportingStatus.Finished)
                    continue;

                var obligation = _obligationCalculator.Calculate(stake);
                if (obligation.ProfitAnomaly)
                {
                    warnings.Add(
                        $"Stake {stake.StakeReference} ({stake.TokenSymbol}): paid profit exceeds expected full-term profit "
                        + $"(expected {obligation.ExpectedFullTermProfit}, paid {obligation.PaidProfitTotal}); remaining profit clamped to 0.");
                }

                openStakes.Add(new OpenStake(stake, status, obligation));
            }

            var assetUsd = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var planUsd = new Dictionary<int, decimal>();
            var totalUsd = 0m;
            var invalidPriceCount = 0;

            // Share % uses open obligation (principal + remaining profit) at deposit-time USD.
            foreach (var open in openStakes)
            {
                var basis = open.Obligation.OpenObligation;
                var symbol = open.Stake.TokenSymbol ?? string.Empty;

                if (_depositTimeValuator.TryValueUsd(basis, open.Stake.TokenPrice, out var valueUsd))
                {
                    assetUsd[symbol] = assetUsd.GetValueOrDefault(symbol) + valueUsd;
                    planUsd[open.Stake.MonthDuration] = planUsd.GetValueOrDefault(open.Stake.MonthDuration) + valueUsd;
                    totalUsd += valueUsd;
                }
                else
                {
                    invalidPriceCount++;
                }
            }

            if (invalidPriceCount > 0)
            {
                warnings.Add(
                    $"{invalidPriceCount} open stake{(invalidPriceCount == 1 ? "" : "s")} had a missing/zero deposit-time price; "
                    + "obligation-weighted % is incomplete; native per-asset values are unaffected.");
            }

            var byAsset = TreasuryAssets.Universe
                .Select(asset =>
                {
                    var symbol = TreasuryAssets.SymbolOf(asset);
                    var nativeOpenObligation = openStakes
                        .Where(o => string.Equals(o.Stake.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                        .Sum(o => o.Obligation.OpenObligation);

                    var valuedUsd = assetUsd.GetValueOrDefault(symbol);
                    var share = totalUsd == 0m ? 0m : valuedUsd / totalUsd * 100m;

                    return new AssetObligationShare
                    {
                        Asset = symbol,
                        NativeOpenObligation = nativeOpenObligation,
                        ValuedObligationUsd = valuedUsd,
                        SharePercent = share
                    };
                })
                .ToList();

            var plans = openStakes
                .Select(o => o.Stake.MonthDuration)
                .Distinct()
                .OrderBy(d => d)
                .Select(duration =>
                {
                    var planStakes = openStakes
                        .Where(o => o.Stake.MonthDuration == duration)
                        .ToList();

                    var planByAsset = TreasuryAssets.Universe
                        .Select(asset =>
                        {
                            var symbol = TreasuryAssets.SymbolOf(asset);
                            var nativeOpenObligation = planStakes
                                .Where(o => string.Equals(o.Stake.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                                .Sum(o => o.Obligation.OpenObligation);

                            return new PlanAssetObligation
                            {
                                Asset = symbol,
                                NativeOpenObligation = nativeOpenObligation
                            };
                        })
                        .ToList();

                    var valuedUsd = planUsd.GetValueOrDefault(duration);
                    var share = totalUsd == 0m ? 0m : valuedUsd / totalUsd * 100m;

                    return new PlanObligationShare
                    {
                        PlanType = duration,
                        ContractCount = planStakes.Count,
                        ValuedObligationUsd = valuedUsd,
                        SharePercent = share,
                        ByAsset = planByAsset
                    };
                })
                .ToList();

            var result = new AssetCompositionResult
            {
                ReportAsOfMoment = reportAsOfMoment,
                ValuationMethod = ValuationMethod.DepositTimeTokenPrice,
                ValuationNote = _depositTimeValuator.ValuationLabel,
                ValuationComplete = invalidPriceCount == 0,
                ByAsset = byAsset,
                Plans = plans,
                Warnings = warnings
            };

            result.SummaryText = _summaryTextBuilder.BuildAssetCompositionSummary(result);

            return result;
        }

        private static List<AssetObligation> BuildAssetObligations(List<OpenStake> openStakes)
            => TreasuryAssets.Universe
                .Select(asset =>
                {
                    var symbol = TreasuryAssets.SymbolOf(asset);
                    var assetStakes = openStakes
                        .Where(o => string.Equals(o.Stake.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    return new AssetObligation
                    {
                        Asset = symbol,
                        PrincipalObligation = assetStakes.Sum(o => o.Obligation.RemainingPrincipalOwed),
                        ProfitObligation = assetStakes.Sum(o => o.Obligation.RemainingProfitOwed),
                        TotalObligation = assetStakes.Sum(o => o.Obligation.OpenObligation)
                    };
                })
                .ToList();

        // Per-asset rank by total StartAmount entered. No cross-asset combined rank.
        private async Task<List<WalletAssetRank>> ComputeWalletRankingAsync(
            string wallet, int topRankThreshold, List<string> warnings)
        {
            var ranking = new List<WalletAssetRank>();

            try
            {
                var all = await _stakeRepository.AsQueryable()
                    .Where(s => s.State != StakeState.NotRegistered
                             && TreasuryAssets.UniverseSymbols.Contains(s.TokenSymbol))
                    .Select(s => new { s.TokenSymbol, s.WalletAddress, s.StartAmount })
                    .ToListAsync();

                foreach (var asset in TreasuryAssets.Universe)
                {
                    var symbol = TreasuryAssets.SymbolOf(asset);

                    var totals = all
                        .Where(s => string.Equals(s.TokenSymbol, symbol, StringComparison.OrdinalIgnoreCase))
                        .GroupBy(s => (s.WalletAddress ?? string.Empty).ToLower())
                        .Select(g => new { Wallet = g.Key, Entered = g.Sum(x => x.StartAmount) })
                        .OrderByDescending(g => g.Entered)
                        .ThenBy(g => g.Wallet, StringComparer.Ordinal)
                        .ToList();

                    var index = totals.FindIndex(g => g.Wallet == wallet);
                    if (index < 0)
                        continue;

                    var rank = index + 1;
                    ranking.Add(new WalletAssetRank
                    {
                        Asset = symbol,
                        Rank = rank,
                        TotalWalletsConsidered = totals.Count,
                        IsTopN = rank <= topRankThreshold
                    });
                }
            }
            catch
            {
                ranking.Clear();
                warnings.Add("Whale ranking was not computable and has been omitted.");
            }

            return ranking;
        }

        private static MaturityCalendarBucket BuildBucket(List<OpenStake> openStakes)
        {
            var perAsset = TreasuryAssets.Universe
                .Select(asset => BuildAssetTotals(openStakes, TreasuryAssets.SymbolOf(asset)))
                .ToList();

            var byPlan = openStakes
                .Select(o => o.Stake.MonthDuration)
                .Distinct()
                .OrderBy(d => d)
                .Select(duration =>
                {
                    var planStakes = openStakes
                        .Where(o => o.Stake.MonthDuration == duration)
                        .ToList();

                    return new MaturityPlanBreakdown
                    {
                        PlanType = duration,
                        PerAsset = TreasuryAssets.Universe
                            .Select(asset => BuildAssetTotals(planStakes, TreasuryAssets.SymbolOf(asset)))
                            .ToList()
                    };
                })
                .ToList();

            return new MaturityCalendarBucket
            {
                PerAsset = perAsset,
                ByPlan = byPlan
            };
        }

        private static MaturityAssetTotals BuildAssetTotals(List<OpenStake> openStakes, string assetSymbol)
        {
            var assetStakes = openStakes
                .Where(o => string.Equals(o.Stake.TokenSymbol, assetSymbol, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new MaturityAssetTotals
            {
                Asset = assetSymbol,
                Count = assetStakes.Count,
                PrincipalToReturn = assetStakes.Sum(o => o.Obligation.RemainingPrincipalOwed),
                ProfitToPay = assetStakes.Sum(o => o.Obligation.RemainingProfitOwed)
            };
        }

        // Resolved status + obligation for one stake — reused across report paths.
        private sealed class OpenStake(Stake stake, TreasuryReportingStatus status, StakeObligation obligation)
        {
            public Stake Stake { get; } = stake;
            public TreasuryReportingStatus Status { get; } = status;
            public StakeObligation Obligation { get; } = obligation;
        }
    }
}
