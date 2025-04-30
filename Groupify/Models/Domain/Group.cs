using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Group
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int RoomId { get; set; }
    public virtual Room Room { get; set; } = null!;
    
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}