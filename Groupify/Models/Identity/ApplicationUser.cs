using Groupify.Models.Domain;
using Microsoft.AspNetCore.Identity;
namespace Groupify.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName  { get; set; } = null!;
    
    public virtual Insight? Insight  { get; set; }
    
    // 1:N to Room
    public virtual ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
    
    // M:N to Room
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public Insight CreateInsightProfile()
    {
        var insight = new Insight
        {
            ApplicationUserId = this.Id,
            Red = 0,
            Green = 0,
            Blue = 0,
            Yellow = 0,
            ApplicationUser = this,
        };
        
        this.Insight = insight;
        return insight;
    }
}