using Microsoft.AspNetCore.Identity;
namespace Groupify.Models;

// base user: shared props
public abstract class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName  { get; set; } = null!;
}

public class StudentUser : ApplicationUser
{
    // 1:1 insight profile
    public InsightModel InsightProfile { get; set; } = new();
}

public class TeacherUser : ApplicationUser
{
    public string Department { get; set; } = null!;

    // 1:N rooms they’ve created
    public ICollection<RoomModel> CreatedRooms { get; set; } 
        = new List<RoomModel>();
}

public class AdminUser : ApplicationUser
{
    // TODO: add admin‑only methods and props
}