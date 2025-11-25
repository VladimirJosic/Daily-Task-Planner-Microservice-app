using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Common;
using UserService.Common.DTOs;
using UserService.Data.Models;

namespace UserService.Business.Interfaces
{
    public interface IAuthService
    {
        Task<ResultPackage<User>> RegisterAsync(UserDto request);
        Task<ResultPackage<LoginResponseDto>?> LoginAsync(LoginUserDto request);
        Task<ResultPackage<bool>> LogoutAsync(string refreshToken);
        Task<ResultPackage<TokenResponseDto?>> RefreshTokensAsync(RefreshTokenRequestDto request);
        Task<ResultPackage<string>> ResetPassword(string email);
    }
}
