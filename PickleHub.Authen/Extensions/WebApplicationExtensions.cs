using Microsoft.EntityFrameworkCore;
using PickleHub.Authen.Domain.Entities;
using PickleHub.Authen.Domain.Enums;
using PickleHub.Authen.Domain.Repositories;
using PickleHub.Authen.Infrastructure.Persistence;
using PickleHub.Common.Interfaces;

namespace PickleHub.Authen.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task SeedAdminAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            try
            {
                var db = scope.ServiceProvider.GetRequiredService<AuthenDbContext>();
                await db.Database.MigrateAsync();

                var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                if (await userRepo.AnyAdminAsync())
                    return;

                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var email = config["SeedAdmin:Email"] ?? "admin@gmail.com";
                var password = config["SeedAdmin:Password"] ?? "123456";

                var admin = User.CreateVerified(email, BCrypt.Net.BCrypt.HashPassword(password), UserRole.Admin);

                userRepo.Add(admin);
                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] SeedAdminAsync error: {ex.Message}");
            }
        }
    }
}
