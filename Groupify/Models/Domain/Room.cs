using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Room
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string OwnerId { get; set; } = null!; // OwnerId is a GUID thus string
    public virtual ApplicationUser Owner { get; set; } = null!;
    
    [Required, MinLength(2)]
    public string Name { get; set; } = null!;
    
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    
    /*public void AddUser(ApplicationUser user)
    {
        if (Users.Contains(user))
            throw new InvalidOperationException("Student already in room");
        Users.Add(user);
        // student.Rooms.Add(this);
    }
    
    public void RemoveUser(ApplicationUser user)
    {
        if (!Users.Contains(user))
            throw new InvalidOperationException("Student not in room");
        Users.Remove(user);
        // student.Rooms.Remove(this);
    }
    
    public void CreateGroup(int groupSize)
    {
        if (groupSize <= 0) throw new ArgumentException("Group size must be greater than 0");
        var all = Users.ToList();
        for (int i = 0; i < all.Count; i += groupSize)
        {
            var group = new Group { Room = this };
            foreach (var user in all.Skip(i).Take(groupSize))
                group.Users.Add(user);
            Groups.Add(group);
        }
    }*/
}