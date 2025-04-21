using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Groupify.Models.Identity;
namespace Groupify.Models.Domain;

public class Room
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string TeacherProfileUserId { get; set; } = null!;
    public virtual TeacherProfile Teacher { get; set; } = null!;
    
    public virtual ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
    
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    
    public void AddStudent(StudentProfile student)
    {
        if (Students.Contains(student))
            throw new InvalidOperationException("Student already in room");
        Students.Add(student);
        // student.Rooms.Add(this);
    }
    
    public void RemoveStudent(StudentProfile student)
    {
        if (!Students.Contains(student))
            throw new InvalidOperationException("Student not in room");
        Students.Remove(student);
        // student.Rooms.Remove(this);
    }
    
    public void CreateGroup(int groupSize)
    {
        if (groupSize <= 0) throw new ArgumentException("Group size must be greater than 0");
        var all = Students.ToList();
        for (int i = 0; i < all.Count; i += groupSize)
        {
            var group = new Group { Room = this };
            foreach (var student in all.Skip(i).Take(groupSize))
                group.Students.Add(student);
            Groups.Add(group);
        }
    }
}