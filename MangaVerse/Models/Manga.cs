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
        public int? ReleaseYear { get; set; }

        [Required(ErrorMessage = "Описанието е задължителна.")]
        public string? Description { get; set; }

        [RegularExpression(@"(?i).*\.(jpg|jpeg|png)$", ErrorMessage ="Корицата трябва да е във формат JPG или PNG")]
        public string? CoverImageUrl { get; set; }

        [Required(ErrorMessage ="Броят на главите е задължителен")]
        [ChaptersCountPositiveNumberAttribute]
        public int? ChaptersCount { get; set; }

        [Required(ErrorMessage = "Попълването на статус е задължително.")]
        public string? Status { get; set; } 

        // Използваме Enum за да предотвратим въвеждането на невалидни жанрове
        [Required(ErrorMessage ="Жанра на мангата е задължителен")]
        public MangaGenre? Genre { get; set; }

        // Връзка към Главите
        public List<Chapter> Chapters { get; set; } = new List<Chapter>();
    }
}
