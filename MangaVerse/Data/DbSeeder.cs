using Microsoft.AspNetCore.Identity;
using MangaVerse.Models;

namespace MangaVerse.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Създаване само на роля Admin
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // 2. Създаване на Администратор
            var adminEmail = "admin@mangaverse.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Парола: Admin123!
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Начални данни за манга (само ако базата е празна)
            if (!context.Mangas.Any())
            {
                context.Mangas.Add(new Manga
                {
                    Title = "One Piece",
                    Author = "Eiichiro Oda",
                    ReleaseYear = 1997,
                    Genre = MangaGenre.Шонен,
                    Status = "Продължаващо",
                    CoverImageUrl = "/images/covers/one-piece.jpg",
                    Description = "Епично приключение за пирати."
                });
                await context.SaveChangesAsync();
            }
        }
    }
}