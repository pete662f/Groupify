using Microsoft.AspNetCore.Identity;
namespace Groupify.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName  { get; set; } = null!;
    
    public virtual StudentProfile? StudentProfile { get; set; }
    public virtual TeacherProfile? TeacherProfile { get; set; }
    public virtual AdminProfile? AdminProfile { get; set; }
}