using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Insight
{
    [Key, ForeignKey(nameof(ApplicationUser))]
    public string ApplicationUserId { get; set; } = null!;

    public int Red { get; set; }
    public int Green { get; set; }
    public int Blue { get; set; }
    public int Yellow { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; } = null!;
}