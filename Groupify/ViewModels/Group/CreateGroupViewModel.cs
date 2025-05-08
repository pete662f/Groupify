using System.ComponentModel.DataAnnotations;

namespace Groupify.ViewModels.Group;

public class CreateGroupViewModel
{
    [Required(ErrorMessage = "Room ID is required.")]
    public Guid RoomId { get; set; } = Guid.Empty;
    
    [Required(ErrorMessage = "Group size is required.")]
    [Range(1, 100, ErrorMessage = "Group size must be between 1 and 100.")]
    int GroupSize { get; set; } = 0;
    
    
}