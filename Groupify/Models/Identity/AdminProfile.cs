using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Groupify.Models.Identity;

public class AdminProfile
{
    [Key, ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;
    public virtual ApplicationUser User { get; set; } = null!;
}