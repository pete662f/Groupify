using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Room
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    
    [Required]
    public string OwnerId { get; set; } = null!; // OwnerId is a GUID thus string
    public ApplicationUser Owner { get; set; } = null!;
    
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;
    
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    
    public ICollection<Group> Groups { get; set; } = new List<Group>();
}