using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MangaVerse.Data;

var builder = WebApplication.CreateBuilder(args);

// База данни
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity само с роля Admin
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4; // Още по-лесна парола
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddErrorDescriber<MangaVerse.Validation.BulgarianIdentityErrorDescriber>();

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
        (x) => $"Стойността '{x}' е невалидна.");
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(
        (x) => $"Полето {x} трябва да бъде число.");
    options.ModelBindingMessageProvider.SetMissingBindRequiredValueAccessor(
        (x) => $"Стойността за '{x}' липсва.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
        (x, y) => $"Стойността '{x}' не е валидна за {y}.");
    options.ModelBindingMessageProvider.SetUnknownValueIsInvalidAccessor(
        (x) => $"Стойността е невалидна за {x}.");
});

var app = builder.Build();

// Извикване на Seeder-а
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedDefaultData(scope.ServiceProvider);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();