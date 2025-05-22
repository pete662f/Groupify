using Groupify.Models.Domain;
using Microsoft.AspNetCore.Identity;
namespace Groupify.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; init; } = null!;
    public string LastName  { get; init; } = null!;
    
    public Insight? Insight  { get; set; }
    
    // 1:N to Room
    public ICollection<Room> CreatedRooms { get; init; } = new List<Room>();
    
    // M:N to Room
    public ICollection<Room> Rooms { get; init; } = new List<Room>();
    
    // M:N to Group
    public ICollection<Group> Groups { get; init; } = new List<Group>();

    public Insight CreateInsightProfile()
    {
        var insight = new Insight
        {
            ApplicationUserId = this.Id,
            Red = 0,
            Green = 0,
            Blue = 0,
            Yellow = 0,
            WheelPosition = 0,
            ApplicationUser = this,
        };
        
        this.Insight = insight;
        return insight;
    }
}