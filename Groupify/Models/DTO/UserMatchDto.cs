using Groupify.Models.Identity;

namespace Groupify.Models.DTO;

public class UserMatchDto
{
    public ApplicationUser User { get; set; } = null!;
    public float MatchPercentage { get; set; }
}