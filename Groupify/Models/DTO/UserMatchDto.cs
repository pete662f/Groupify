using Groupify.Models.Identity;

namespace Groupify.Models.DTO;

// DTO means Data Transfer Object
public class UserMatchDto
{
    public ApplicationUser User { get; set; } = null!;
    public float MatchPercentage { get; set; }
}