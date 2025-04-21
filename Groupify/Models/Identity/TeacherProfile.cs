using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Domain;
namespace Groupify.Models.Identity;

public class TeacherProfile
{
    [Key, ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;
    
    public virtual ApplicationUser User { get; set; } = null!;
    public string Department { get; set; } = null!;
    
    // 1:N to Room
    public virtual ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
}