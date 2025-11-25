using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UserService.Business.Interfaces;
using UserService.Common;
using UserService.Common.DTOs;
using UserService.Common.ENUMs;
using UserService.Data;
using UserService.Data.Models;

namespace UserService.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context, IConfiguration configuration, PasswordHasher<User> passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }


        public async Task<ResultPackage<LoginResponseDto>> LoginAsync(LoginUserDto request)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
                return new ResultPackage<LoginResponseDto>(
                    ResultStatus.Unauthorized,
                    "Invalid username or password"
                );

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return new ResultPackage<LoginResponseDto>(
                    ResultStatus.Unauthorized,
                    "Invalid username or password"
                );
            }

            string token = CreateToken(user);

            RefreshToken refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = GenerateRefreshToken(),
                ExpiresOnUtc = DateTime.UtcNow.AddHours(2)
            };

            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            LoginResponseDto response = new LoginResponseDto
            {
                UserId = user.Id,
                AccessToken = token,
                RefreshToken = refreshToken.Token
            };

            return new ResultPackage<LoginResponseDto>(response, ResultStatus.OK, "Login successful");
        }

        public async Task<ResultPackage<bool>> LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (token is null)
            {
                return new ResultPackage<bool>(ResultStatus.BadRequest, "Invalid refresh token");
            }

            _context.RefreshTokens.Remove(token);
            await _context.SaveChangesAsync();

            return new ResultPackage<bool>(true);
        }

        public async Task<ResultPackage<TokenResponseDto?>> RefreshTokensAsync(RefreshTokenRequestDto request)
        {
            RefreshToken? refreshToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

            if (refreshToken is null || refreshToken.ExpiresOnUtc < DateTime.UtcNow)
            {
                return new ResultPackage<TokenResponseDto?>(ResultStatus.BadRequest, "Invalid refresh token");
            }

            string accessToken = CreateToken(refreshToken.User);

            refreshToken.Token = GenerateRefreshToken();
            refreshToken.ExpiresOnUtc = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            TokenResponseDto response = new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            };

            return new ResultPackage<TokenResponseDto?>(response);
        }

        public async Task<ResultPackage<User>> RegisterAsync(UserDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new ResultPackage<User>(ResultStatus.BadRequest, "Username and password are required");
            }

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return new ResultPackage<User>(ResultStatus.Conflict, "Username already exists");
            }

            User user = new User
            {
                Name = request.Name,
                LastName = request.LastName,
                Username = request.Username,
                Email = request.Email
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                return new ResultPackage<User>(user, ResultStatus.Created, "User registered successfully");
            }
            catch (DbUpdateException ex)
            {
                return new ResultPackage<User>(ResultStatus.InternalServerError, "Database error occurred while registering user");
            }
            catch (Exception ex)
            {
                return new ResultPackage<User>(ResultStatus.InternalServerError, "An unexpected error occurred");
            }
        }

        public async Task<ResultPackage<string>> ResetPassword(string email)
        {
            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                return new ResultPackage<string>(ResultStatus.NotFound,
                                                "No user found with that email address.");
            }

            string newPassword = GenerateSecureRandomString(10);
            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

            try
            {
                await _context.SaveChangesAsync();

                return new ResultPackage<string>(newPassword,
                                                 ResultStatus.OK,
                                                "Password reset successfully. Use the provided password to log in.");
            }
            catch
            {
                return new ResultPackage<string>(ResultStatus.InternalServerError,
                                                "An unexpected error occurred while resetting the password.");
            }

        }



        private string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["AppSettings:Token"])); // Use indexer syntax to access configuration values

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new JwtSecurityToken(
                issuer: _configuration["AppSettings:Issuer"], // Use indexer syntax here as well
                audience: _configuration["AppSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }

        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        private string GenerateSecureRandomString(int length)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var randomBytes = new byte[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var result = new StringBuilder(length);
            foreach (byte b in randomBytes)
            {
                result.Append(validChars[b % validChars.Length]);
            }

            return result.ToString();
        }
    }
}
