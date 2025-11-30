using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Data
{
    public static class AddAdminUser
    {
        public static async Task AddAdminAsync(LibraryDbContext context, string username, string password, string email)
        {
            // Check if user already exists
            if (await context.Users.AnyAsync(u => u.Username == username))
            {
                Console.WriteLine($"User '{username}' already exists!");
                return;
            }

            var adminUser = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Email = email,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"Admin user '{username}' created successfully!");
        }
    }
}



