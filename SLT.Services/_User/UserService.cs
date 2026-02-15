using Microsoft.AspNetCore.Mvc;
using SLT.Domain.Collections;
using SLT.Domain.Repositories.Contracts;
using SLT.Services._User.DTOs;
using System.Security.Claims;
using System.Threading;
using Utilities.Constants;
using Utilities.Enums;
using Utilities.Exceptions.Common;
using Utilities.Services.Contracts;
using Utilities.Utilities;
using MongoDB.Driver.Linq;
using static Utilities.Constants.RegisterMode;

namespace SLT.Services._User
{
    public class UserService ( IJwtService _jwtService,
        JwtServiceSettings _jwtSettings,
        IPasswordService _passwordService,
        IUserRepository _userRepository) : IUserService, IScopedDependency
    {


        public async Task<ActionResult> LoginAsync(LoginUpdate update, string ip)
        {
            ValidateClientInfo(update.ClientId, update.ClientSecret);

            var user = await _userRepository.AsQueryable()
                .Where(q => q.UserName.ToLower() == update.UserName.ToLower() && q.Role == UserRole.Admin).FirstOrDefaultAsync();

            if (user == null) throw new NotFoundException("user not found");

            if (!_passwordService.Verify(update.Password, user.PasswordHash))
                throw new NotFoundException("user not found");

            await AddLoginDateToUser(user);

            return Authenticate(user);
        }


        public async Task<bool> CreateAdminAsync()
        {
            try
            {
                var publicKey = Guid.NewGuid().ToString("N");
                var securityStamp = Guid.NewGuid().ToString("N");

                var newUser = new User
                {
                    Role = UserRole.Admin,
                    UserName = "master.sltco",
                    LoginDates = [],
                    Permissions = ["SSL1#A#"],
                    Status = UserStatus.Active,
                    WalletAddress = "adminWallet",
                    UserPublicKey= publicKey,
                    SecurityStamp = securityStamp,
                    PasswordHash = _passwordService.Hash("SlTtLs#2025"),
                };
                await _userRepository.InsertOneAsync(newUser);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }



        /// <summary>
        /// this method use for creating jwt
        /// </summary>
        /// <param name="tabletUniqeId"></param>
        /// <param name="tabletData"></param>
        /// <returns></returns>
        private ActionResult Authenticate(User user)
           => new JsonResult(_jwtService.Generate(GetClaimsAsync(user)));


        /// <summary>
        /// for create the cliams of jwt
        /// </summary>
        /// <param name="tabletUniqeId"></param>
        /// <param name="tabletData"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        private IEnumerable<Claim> GetClaimsAsync(User user)
        {
            try
            {
                var claims = new List<Claim>
             {
                 new(Claims.WalletAddress.ToDisplay(),user.WalletAddress ?? "no wallet"),
                 new(Claims.PublicKey.ToDisplay(),user.UserPublicKey.ToString()),
                 new(Claims.SecurityStamp.ToDisplay(),user.SecurityStamp.ToString()),
                 new(Claims.UserStatus.ToDisplay(),user.Status.ToString()),
                 new(Claims.UserType.ToDisplay(),user.Role == UserRole.Customer ? UserType.User.ToString() : UserType.Admin.ToString()),
             };

                claims.AddRange(user.Permissions.Select(permission =>
                    new Claim(Claims.Permission.ToDisplay(), permission)));

                return claims;
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }
        }

        /// <summary>
        /// for adding last login time, just keep last 20 record
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="BaseException"></exception>
        private async Task<Domain.Collections.User> AddLoginDateToUser(Domain.Collections.User user)
        {
            try
            {
                if (user.LoginDates == null || user.LoginDates.Count == 0)
                {
                    user.LoginDates = new List<DateTime> { DateTime.UtcNow };
                    return user;
                }

                user.LoginDates.Add(DateTime.UtcNow);

                var orderedDates = user.LoginDates
                    .OrderByDescending(x => x)
                    .Take(20)
                    .ToList();

                user.LoginDates = orderedDates;

                await _userRepository.ReplaceOneAsync(user);

                return user;
            }
            catch (Exception ex)
            {
                throw new BaseException(ex.Message);
            }

        }



        /// <summary>
        /// for validate the ClientInformation , OAuth2 verification
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <exception cref="BadRequestException"></exception>
        private void ValidateClientInfo(string clientId, string clientSecret)
        {
            if (!clientId.HasValue() ||
                !clientSecret.HasValue() ||
                !_jwtSettings.ClientInfo.ContainsKey(clientId.ToLower()) ||
                !_jwtSettings.ClientInfo[clientId.ToLower()].Equals(clientSecret, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException(ApiResultStatusCode.OAuth.ToDisplay());
        }



    }
}
