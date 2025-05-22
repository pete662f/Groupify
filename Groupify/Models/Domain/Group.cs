using System.ComponentModel.DataAnnotations;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Group
{
    [Key]
    public Guid Id { get; internal init; } = Guid.NewGuid();
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Group number must be greater than 0")]
    public int GroupNumber { get; init; }
    
    [Required]
    public Guid RoomId { get; init; }
    public Room Room { get; init; } = null!;
    
    public ICollection<ApplicationUser> Users { get; init; } = new List<ApplicationUser>();
}