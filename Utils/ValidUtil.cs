using System.ComponentModel.DataAnnotations;

namespace RP.SOI.DotNet.Utils
{

    public class DateGreaterThan : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThan(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            ErrorMessage = ErrorMessageString;
            var currentValue = (DateTime?)value;

            if (currentValue == null)
                return ValidationResult.Success!;

            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
            if (property == null)
                throw new ArgumentException("Property with this name not found");

            var comparisonValue = (DateTime?)property.GetValue(validationContext.ObjectInstance);

            if (comparisonValue == null)
                return ValidationResult.Success!;

            if (currentValue > comparisonValue)
                return ValidationResult.Success!;
            else
                return new ValidationResult(ErrorMessage);
        }
    }
}