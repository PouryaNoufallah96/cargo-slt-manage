# SLT Manage

Admin/reporting API for CargoPay. Reads Mongo data written by **slt.api**. Username/password → JWE. **No on-chain clients** — chain fields are persisted data only.

## Domain

**Order**, **Invoice**, **TransactionLog**, **BlockchainEventType** — same persisted shapes as slt.api; reports aggregate by `PaymentHash` / wallets.

**User**:
Admin account (`UserName`, `PasswordHash` BCrypt, permission codes, `Role`). Not wallet auth.
_Avoid_: customer, wallet user

**Log** / **RequestLog**:
Event log and per-request audit (body + headers).

**SLT / LUSD**:
BSC tokens in inherited config; not consumed for writes in this repo.

## Treasury Program (Stake)

**Treasury Program**:
The admin/reporting name for the **staking** product. The underlying persisted entity is the
**Stake** — there is no separate "Treasury" or "Deposit" collection. "Treasury contract" = one **Stake**.
_Avoid_: Deposit, Treasury contract (as a distinct entity)

**Stake**:
A staking contract written by slt.api into the shared `Stakes` collection. Carries principal
(`StartAmount` original, `TokenAmount` remaining), plan length (`MonthDuration`), `StartMoment`/
`EndMoment`, monthly profit, withdrawn totals, and `State`. Asset universe is **GOLDGR** and **LUSD** only.

**StakeState** (persisted lifecycle, owned by slt.api — never written here):
`NotRegistered` (pending intent, not yet on-chain; pruned after 7 days) → `Active` (on-chain confirmed)
→ `Finished` (principal fully redeemed). `Canceled` exists in the enum but is **never assigned** (dead).

**Reporting status** (derived in this repo only, from `State` + `EndMoment` + the request's
`reportAsOfMoment`; does **not** mutate `State`):
- **WithinTerm** — `State=Active` and `EndMoment > reportAsOfMoment`.
- **MaturedUnredeemed** (a.k.a. **Overdue / Due Now**) — `State=Active` and `EndMoment <= reportAsOfMoment`;
  an unredeemed liability, reported separately from future maturities.
- **Finished** — `State=Finished` (principal redeemed).
"Open / unredeemed principal" = WithinTerm + MaturedUnredeemed.

**reportAsOfMoment**:
A single UTC instant captured once per report request; the boundary for all WithinTerm/Matured math
(`EndMoment <= reportAsOfMoment` ⇒ matured).

**Contract anchor dates** (which timestamp means what in Treasury reports):
- `StartMoment` — **canonical** anchor: inflow, "new contracts (registered)", generic time filter.
- `EndMoment` — maturity / obligation bucketing.
- `Withdrawal.CreatedMoment` — paid-profit and finished-in-period (historical) anchor.
- `RegisterMoment` / `CreatedMoment` — informational/audit only, never the reporting anchor.
Range filters are from-inclusive / to-exclusive (`>= from`, `< to`).

**Expected full-term profit**:
`EachMonthProfit × MonthDuration` — the total contractual profit of a stake. Derived (never stored).

**Paid profit**:
`TotalProfitWithdrawn + TotalProfitOfAmountWithdrawn` (pure-profit claims **plus** profit paid out
alongside principal). Using only `TotalProfitWithdrawn` under-reports.

**Remaining contractual profit** (a.k.a. **Unpaid full-term profit**):
`Expected full-term profit − Paid profit`, clamped to ≥ 0 (a negative value is a data anomaly to flag,
not hide). It is the unpaid part of the **full contractual** profit per Mongo — **NOT** chain-accrued-to-date
profit (which is on-chain and out of scope).
_Avoid_: accrued profit owed, accrued-to-date profit

**Deposit-time valuation**:
Cross-asset value-weighted percentages (asset composition, capital distribution) value each stake at
`amount × Stake.TokenPrice`, where `TokenPrice` is a **USD** price (`base_token_price_usd`) snapshotted
at deposit and never refreshed. Always labeled "valued at deposit-time price, not mark-to-market". If
`TokenPrice` is missing/zero, native per-asset values are still returned and the percentage is flagged
incomplete — never silently treated as zero. All other metrics stay per-asset (no valuation).

**Open obligation** (per stake):
`Remaining principal owed (= TokenAmount) + Remaining contractual profit`. Computed only for open
(unredeemed) stakes. Bucketed as **Due Now / Overdue** (MaturedUnredeemed) or **Future** (WithinTerm,
by `EndMoment`). The same formula feeds every Treasury report and export.

**SummaryText**:
A deterministic, server-built, human-readable management sentence on each report result model, derived
purely from the computed metrics (no LLM, no external calls). Identical in JSON and exports.

**Warnings** (data-quality):
Per-report flags for anomalies the report must surface instead of hiding (e.g. invalid/zero `TokenPrice`
excluded from a percentage; missing Withdrawal ledger rows; `Paid profit > Expected full-term profit`).
Native per-asset numbers are always still returned.

## Framework

**Monjo**, **BaseDocument**, **Soft-delete**, **ApiResult**, **Permission code**, **JWE**, **ApplicationId/Nonce/Signature**, **MasterSignature** — same N-tier conventions as slt.api.

**Manage host**:
`SLT.Manage` — `_User`, `_Report`, `_Log` modules only.
