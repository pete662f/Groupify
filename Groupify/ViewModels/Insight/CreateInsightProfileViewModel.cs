using System.ComponentModel.DataAnnotations;

namespace Groupify.ViewModels.Insight;

public class CreateInsightProfileViewModel
{
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
}