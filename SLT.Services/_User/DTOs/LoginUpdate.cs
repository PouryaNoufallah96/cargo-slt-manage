using Utilities.Attributes;

namespace SLT.Services._User.DTOs
{
    public class LoginUpdate
    {
        [StringInputValidation(maxLength: 50)] public string UserName { get; set; }
        [StringInputValidation(maxLength: 50)] public string Password { get; set; }
        [StringInputValidation(maxLength: 50)] public string ClientId { get; set; }
        [StringInputValidation(maxLength: 50)] public string ClientSecret { get; set; }
    }


}
