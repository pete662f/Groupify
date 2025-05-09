using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Groupify.ViewModels.Room;

public class ChangeRoomNameViewModel
{
    [Required(ErrorMessage = "Room ID is required.")]
    [JsonPropertyName("roomId")]
    public Guid RoomId { get; set; }
    [Required(ErrorMessage = "Room name is required.")]
    [StringLength(100, ErrorMessage = "Room name cannot be longer than 100 characters.")]
    [MinLength(2, ErrorMessage = "Room name must be at least 2 characters long.")]
    [DataType(DataType.Text)]
    [JsonPropertyName("newName")]
    public string NewName { get; set; } = string.Empty;
}