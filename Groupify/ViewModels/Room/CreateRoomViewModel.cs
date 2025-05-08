using System.ComponentModel.DataAnnotations;

namespace Groupify.ViewModels.Room;

public class CreateRoomViewModel
{
    [Required(ErrorMessage = "Room name is required.")]
    [StringLength(100, ErrorMessage = "Room name cannot be longer than 100 characters.")]
    [DataType(DataType.Text)]
    [Display(Name = "Room Name")]
    [MinLength(2, ErrorMessage = "Room name must be at least 2 characters long.")]
    public string Name { get; set; } = "";
}