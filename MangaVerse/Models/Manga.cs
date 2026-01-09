using MangaVerse.Validation;
using System.ComponentModel.DataAnnotations;
namespace MangaVerse.Models
{
    public class Manga 
    {
        public int Id { get; set; }

        [Required(ErrorMessage ="Заглавието е задължително")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage ="Авторът е задължителен")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Дата на издание е задължителна.")]
        [ReleaseYearRangeAttribute]
        public int ReleaseYear { get; set; }
        public string? Description { get; set; }

        [RegularExpression(@"(?i).*\.(jpg|jpeg|png)$", ErrorMessage ="Корицата трябва да е във формат JPG или PNG")]
        public string? CoverImageUrl { get; set; }
        [Required(ErrorMessage ="Броят на главите е задължителен")]
        public int ChaptersCount { get; set; }
        public string? Status { get; set; } //Продължаващо или Завършено

        // Използваме Enum за да предотвратим въвеждането на невалидни жанрове
        [Required(ErrorMessage ="Жанра на мангата е задължителен")]
        public MangaGenre Genre { get; set; }
    }
}
