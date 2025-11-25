using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Business.Interfaces;
using UserService.Business.Services;
using UserService.Common.DTOs;
using UserService.Common.ENUMs;

namespace UserService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult> Register([FromBody] UserDto request)
        {
            var result = await _authService.RegisterAsync(request);

            return result.Status switch
            {
                ResultStatus.Created => CreatedAtAction(nameof(Register), new
                {
                    user = result.Data,
                    message = result.Message
                }),
                ResultStatus.Conflict => Conflict(result.Message),
                ResultStatus.BadRequest => BadRequest(result.Message),
                _ => StatusCode(StatusCodes.Status500InternalServerError, result.Message)
            };
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginUserDto request)
        {
            var result = await _authService.LoginAsync(request);

            if (result.Status != ResultStatus.OK || result.Data is null)
            {
                return Unauthorized(result.Message ?? "Invalid username or password.");
            }

            return Ok(result.Data);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            var result = await _authService.LogoutAsync(request.RefreshToken);

            return result.Status switch
            {
                ResultStatus.OK => Ok(new { Message = "Logged out successfully" }),
                ResultStatus.BadRequest => BadRequest(result.Message),
                _ => StatusCode(StatusCodes.Status500InternalServerError, result.Message)
            };
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokensAsync(request);

            if (result.Status != ResultStatus.OK || result.Data is null)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("Email address is required.");
            }

            var result = await _authService.ResetPassword(email);

            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result.Message ?? "User not found.");
            }

            if (result.Status != ResultStatus.OK || result.Data is null)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    result.Message ?? "An error occurred while resetting the password."
                );
            }

            // Za sada (dok nemaš EmailService), samo vraćaš novu lozinku u response.
            // Kasnije ovde ubaciš slanje mail-a i ukloniš lozinku iz responsea.
            return Ok(new
            {
                Message = "Password has been reset successfully.",
                NewPassword = result.Data // skloni ovo kad uvedeš email slanje
            });
        }
    }
}
