using System.ComponentModel.DataAnnotations;

namespace Groupify.ViewModels.Group;

public class CreateGroupViewModel
{
    [Required(ErrorMessage = "Room ID is required.")]
    public Guid RoomId { get; set; }
    
    [Required(ErrorMessage = "Group size is required.")]
    [Range(2, 100, ErrorMessage = "Group size must be between 2 and 100.")]
    public int GroupSize { get; set; }
    
    
}