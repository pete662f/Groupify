using System.ComponentModel.DataAnnotations;

namespace Groupify.ViewModels;

public class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(2)]
    public string FirstName { get; set; } = null!;

    [Required, MinLength(2)]
    public string LastName { get; set; } = null!;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = null!;

    [Required]
    public string Role { get; set; } = null!; // "Student", "Teacher", "Admin"

    // only for teachers
    public string? Department { get; set; }
}