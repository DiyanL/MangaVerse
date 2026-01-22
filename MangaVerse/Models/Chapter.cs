using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MangaVerse.Models
{
    public class Chapter
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заглавието на главата е задължително")]
        [Display(Name = "Заглавие")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Дата на добавяне")]
        public DateTime DateAdded { get; set; } = DateTime.Now;

        // Foreign Key
        public int MangaId { get; set; }

        [ForeignKey("MangaId")]
        public Manga? Manga { get; set; }

        // JSON string to store sorted list of image paths
        public string? ImagePathsJson { get; set; }
    }
}
