using Microsoft.AspNetCore.Mvc;
using SLT.Services._User.DTOs;

namespace SLT.Services._User
{
    public interface IUserService
    {
        Task<ActionResult> LoginAsync(LoginUpdate update, string ip);
        Task<bool> CreateAdminAsync();
    }
}
