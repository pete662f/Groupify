using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Room
{
    [Key]
    public Guid Id { get; internal init; } = Guid.NewGuid();
    
    [Required]
    public string OwnerId { get; init; } = null!; // OwnerId is a GUID thus string
    public ApplicationUser Owner { get; init; } = null!;
    
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;
    
    public ICollection<ApplicationUser> Users { get; init; } = new List<ApplicationUser>();
    
    public ICollection<Group> Groups { get; init; } = new List<Group>();
}