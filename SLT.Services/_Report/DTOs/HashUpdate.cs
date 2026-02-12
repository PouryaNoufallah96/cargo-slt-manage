using Utilities.Attributes;

namespace SLT.Services._Report.DTOs
{
    public class HashUpdate
    {
        [StringInputValidation(minLength:20)] public string Hash { get; set; }
    }

    public class  WalletUpdate
    {
        [StringInputValidation(minLength:20)] public string Wallet { get; set; }
    }


    public class OrderIdUpdate
    {
        [StringInputValidation(maxLength: 32, minLength: 10)] public string OrderOrTransferId { get; set; }
    }
}
    