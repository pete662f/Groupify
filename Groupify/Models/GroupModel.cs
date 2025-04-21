using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace Groupify.Models;

public class GroupModel
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int RoomModelId { get; set; }
    [ForeignKey(nameof(RoomModelId))]
    public RoomModel Room { get; set; } = null!;
    
    public ICollection<StudentUser> Students { get; set; } = new List<StudentUser>();
    
    public void AddStudent(StudentUser student) => Students.Add(student);
    public void RemoveStudent(StudentUser student) => Students.Remove(student);
}