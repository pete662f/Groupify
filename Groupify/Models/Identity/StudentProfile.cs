using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Domain;
namespace Groupify.Models.Identity;

public class StudentProfile
{
    [Key, ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Insight? Insight  { get; set; }

    // M:N to Room
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}