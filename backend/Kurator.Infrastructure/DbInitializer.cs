using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Kurator.Infrastructure.Data;
using Kurator.Core.Entities;
using Kurator.Core.Enums;
using Kurator.Core.Interfaces;

namespace Kurator.Infrastructure;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // Apply migrations
        await context.Database.MigrateAsync();

        // Check if admin already exists
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            return; // Admin already exists
        }

        // Create default admin
        var admin = new User
        {
            Login = "admin",
            PasswordHash = passwordHasher.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            IsFirstLogin = true,
            IsActive = true,
            MfaEnabled = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync();

        Console.WriteLine("Default admin user created:");
        Console.WriteLine("Login: admin");
        Console.WriteLine("Password: Admin123!");
        Console.WriteLine("IMPORTANT: Setup MFA on first login!");
    }
}
