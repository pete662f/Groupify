namespace Groupify.ViewModels.Room;

public class DetailsRoomViewModel
{
    // Did not use "using Groupify.Models.Domain;" to avoid confusion with the namespace
    public Models.Domain.Room Room { get; set; } = null!;
    public IEnumerable<Models.Domain.Group> Groups { get; set; } = Array.Empty<Models.Domain.Group>();
}