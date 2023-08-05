using System.ComponentModel.DataAnnotations;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Confirm password is required.")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = null!;
    [RegularExpression(@"^[89]\d{7}$", ErrorMessage = "The input must start with 8 or 9 and have exactly 8 digits.")]
    public string PhoneNumber { get; set; } = null!;
    public int User { get; set; } 
}