using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Groupify.Models;

public class RoomModel
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string TeacherModelId { get; set; }
    [ForeignKey(nameof(TeacherModelId))]
    public TeacherUser Teacher { get; set; } = null!;
    
    public ICollection<StudentUser> Students { get; set; } = new List<StudentUser>();
    
    public ICollection<GroupModel> Groups { get; set; } = new List<GroupModel>();
    
    public void AddStudent(StudentUser student)
    {
        if (Students.Contains(student))
            throw new InvalidOperationException("Student already in room");
        Students.Add(student);
        // student.Rooms.Add(this);
    }
    
    public void RemoveStudent(StudentUser student)
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
            var group = new GroupModel { Room = this };
            foreach (var student in all.Skip(i).Take(groupSize))
                group.Students.Add(student);
            Groups.Add(group);
        }
    }
}