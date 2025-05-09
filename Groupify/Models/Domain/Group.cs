using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Group
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Group number must be greater than 0")]
    public int GroupNumber { get; set; }
    
    [Required]
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}