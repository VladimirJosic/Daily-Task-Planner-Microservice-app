using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class UsersService(AppDbContext _context, PasswordHasher<User> passwordHasher) : IUsersService
    {
        public async Task<ResultPackage<User>> CreateUserAsync(CreateUserDto newUser)
        {
            User? existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == newUser.Username);

            if (existingUser is not null)
            {
                return new ResultPackage<User>(ResultStatus.BadRequest, "User with that username already exists");
            }

            User user = new User
            {
                Name = newUser.Name,
                LastName = newUser.LastName,
                Username = newUser.Username,
                Email = newUser.Email,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, newUser.Password);

            await _context.Users.AddAsync(user);

            try
            {
                await _context.SaveChangesAsync();
                return new ResultPackage<User>(user, ResultStatus.Created, "User created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save to database: {0}", ex.InnerException);
                return new ResultPackage<User>(ResultStatus.InternalServerError, "Error saving to databse");
            }
        }

        public async Task<List<User>> GetAllUsers()
        {
            List<User> result = await _context.Users.ToListAsync();
            return result;
        }

        public async Task<User?> GetUserAsync(int id)
        {
            User? user = await _context.Users.FindAsync(id);

            return user;
        }

        public async Task<User?> SearchUserAsync(string? username, string? email)
        {
            try
            {
                return await _context.Users
                   .FirstOrDefaultAsync(u =>
                       (username != null && u.Username.ToLower() == username.ToLower()) ||
                       (email != null && u.Email.ToLower() == email.ToLower()));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error searching for user: {0}", ex.Message);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto updatedUser)
        {
            var existingUser = await _context.Users.FindAsync(id);

            if (existingUser == null || updatedUser == null)
                return false;

            existingUser.Name = updatedUser.Name;
            existingUser.LastName = updatedUser.LastName;
            existingUser.Username = updatedUser.Username;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return false;
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
