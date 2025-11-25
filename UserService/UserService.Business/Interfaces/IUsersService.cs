using UserService.Common;
using UserService.Common.DTOs;
using UserService.Data.Models;

namespace UserService.Business.Interfaces
{
    public interface IUsersService
    {
        public Task<ResultPackage<User>> CreateUserAsync(CreateUserDto newUser);
        public Task<User?> GetUserAsync(int id);
        public Task<List<User>> GetAllUsers();
        Task<bool> UpdateUserAsync(int id, UpdateUserDto updatedUser);
        public Task<User?> SearchUserAsync(string? username, string? email);
        public Task<bool> DeleteUserAsync(int id);
    }
}
