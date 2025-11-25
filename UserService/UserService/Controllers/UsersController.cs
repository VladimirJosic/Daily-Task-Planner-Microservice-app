using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Business.Interfaces;
using UserService.Common.DTOs;
using UserService.Common.ENUMs;
using UserService.Common;
using UserService.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UserService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _usersService;

        public UsersController(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllUsers()
        {
            List<User> users = await _usersService.GetAllUsers();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            User? user = await _usersService.GetUserAsync(id);

            if (user is null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Post([FromBody] CreateUserDto newUser)
        {
            if (string.IsNullOrEmpty(newUser.Email))
            {
                return BadRequest("Email required");
            }

            if (string.IsNullOrEmpty(newUser.Username))
            {
                return BadRequest("Username required");
            }

            ResultPackage<User> response = await _usersService.CreateUserAsync(newUser);

            if (response.Status == ResultStatus.BadRequest)
            {
                return BadRequest(response.Message);
            }

            if (response.Status == ResultStatus.InternalServerError)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }

            return Created($"api/users/{response.Data?.Id}", response.Data);
        }

        [HttpPut("update")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDto updatedUser)
        {
            var idClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim == null)
                return Unauthorized();

            var userId = int.Parse(idClaim.Value);

            if (updatedUser == null)
            {
                return BadRequest("Invalid user data.");
            }

            var result = await _usersService.UpdateUserAsync(userId, updatedUser);

            if (!result)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            return NoContent(); // 204 No Content – success
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var idClaim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (idClaim == null)
                return Unauthorized();

            var userId = int.Parse(idClaim.Value);
            var user = await _usersService.GetUserAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _usersService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound($"User with ID {id} not found.");
            }
            return NoContent(); // 204 No Content – success
        }

        [HttpGet("Search_user")]
        public async Task<IActionResult> SearchUser([FromQuery] string? username, [FromQuery] string? email)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(email))
            {
                return BadRequest("You must provide either a username or an email.");
            }

            var user = await _usersService.SearchUserAsync(username, email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }
    }
}
