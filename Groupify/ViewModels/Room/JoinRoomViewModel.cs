using System.ComponentModel.DataAnnotations;

namespace Groupify.ViewModels.Room;

public class JoinRoomViewModel
{
    [Required(ErrorMessage = "Room ID is required.")]
    public Guid RoomId { get; set; } = Guid.Empty;
}