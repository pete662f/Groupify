namespace Groupify.ViewModels.Group;

public class GroupsViewModel
{
    public IEnumerable<Models.Domain.Group> Groups { get; set; } = new List<Models.Domain.Group>();
}