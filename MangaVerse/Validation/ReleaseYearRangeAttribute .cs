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
                return new ValidationResult("Invalid year.");

            int currentYear = DateTime.Today.Year;

            if (year < 1900 || year > currentYear)
            {
                return new ValidationResult(
                    $"Release year must be between 1900 and {currentYear}."
                );
            }

            return ValidationResult.Success;
        }
    }

}