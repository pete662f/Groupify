using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Insight
{
    [Key, ForeignKey(nameof(ApplicationUser))]
    public string ApplicationUserId { get; set; } = null!;

    // Insight color energies 0 to 6
    [Required]
    [Range(0, 6, ErrorMessage="Energy must be between 0 and 6")]
    public float Red { get; set; }
    [Required]
    [Range(0, 6, ErrorMessage="Energy must be between 0 and 6")]
    public float Green { get; set; }
    [Required]
    [Range(0, 6, ErrorMessage="Energy must be between 0 and 6")]
    public float Blue { get; set; }
    [Required]
    [Range(0, 6, ErrorMessage="Energy must be between 0 and 6")]
    public float Yellow { get; set; }
    
    // Insight wheel position
    [Required]
    [Range(0, Int32.MaxValue, ErrorMessage="Wheel position must be a positive integer")]
    public int WheelPosition { get; set; }
 
    public Vector4 ToVector4() => new Vector4(Blue, Green, Yellow, Red );

    public ApplicationUser ApplicationUser { get; set; } = null!;
}