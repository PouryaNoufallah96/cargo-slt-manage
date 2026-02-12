using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SLT.Services._User;
using SLT.Services._User.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using Utilities.Api;
using Utilities.Attributes;
using Utilities.Filters;

namespace SLT.Manage.Controllers.V1
{

    [ApiController]
    [ApiResultFilter]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController(IUserService _userService) : ApiBaseController
    {
        [HttpPost("[action]")]
        [CustomRateLimit(maxAttemptsCount: 50)]
        [SwaggerOperation(Summary = "For getting JWT token", Tags = ["Auth"])]
        public async Task<ActionResult> LoginAsync([FromBody] LoginUpdate update)
        {
            return await _userService.LoginAsync(update, Ip);
        }

        //[HttpPost("[action]")]
        //[CustomRateLimit(maxAttemptsCount: 50)]
        //[SwaggerOperation(Summary = "temp", Tags = ["Auth"])]
        //public async Task<bool> CreateAdminAsync()
        //{
        //    return await _userService.CreateAdminAsync(CancellationToken.None);
        //}
    }
}
