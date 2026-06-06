namespace SLT.Services._Treasury.Calculators
{
    public interface IDepositTimeValuator
    {
        bool TryValueUsd(decimal amount, decimal tokenPrice, out decimal valueUsd);

        string ValuationLabel { get; }
    }

    public enum ValuationMethod { DepositTimeTokenPrice }
}
