using Microsoft.AspNetCore.Identity;

namespace MangaVerse.Models
{
    public class ApplicationUser : IdentityUser
    {
        public override string? Email { get => base.Email; set => base.Email = value; }
        public override string? UserName { get => base.UserName; set => base.UserName = value; }
    }
}
