using System.ComponentModel.DataAnnotations;

namespace MangaVerse.Validation
{
    public class ChaptersCountPositiveNumberAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is int chaptersCount)
            {
                if (chaptersCount <= 0)
                {
                    return new ValidationResult("Броят на главите трябва да бъде положително число.");
                }
            }
            else
            {
                return new ValidationResult("Невалиден тип данни за брой глави.");
            }

            return ValidationResult.Success;
        }
    }
}