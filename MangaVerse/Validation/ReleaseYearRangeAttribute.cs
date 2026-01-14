using System;
using System.ComponentModel.DataAnnotations;
namespace MangaVerse.Validation
{
    public class ReleaseYearRangeAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null) //след проверката дали съществува, проверяваме дали е валидна 
                return ValidationResult.Success; 

            if (value is not int year)
                return new ValidationResult("Невалидна година");

            int currentYear = DateTime.Today.Year;

            if (year < 1900 || year > currentYear)
            {
                return new ValidationResult(
                    $"Годината на издаване трябва да бъде между 1900 и {currentYear}."
                );
            }

            return ValidationResult.Success;
        }
    }

}