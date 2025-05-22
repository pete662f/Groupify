using System.Numerics;
using Groupify.Models.Domain;

namespace Groupify.Data.Services.Interfaces;

public interface IGroupService
{
    Task<Vector4> GroupInsightAsync(Guid groupId);
    Task<Group> GetGroupByIdAsync(Guid groupId);
    Task CreateGroupsAsync(Guid roomId, int groupSize);
    Task<IEnumerable<Group>> GetGroupsByRoomIdAsync(Guid roomId);
    Task<IEnumerable<Group>> GetGroupsByUserIdAsync(string userId);
    Task RemoveGroupAsync(Guid groupId);
    Task RemoveAllGroupsByRoomIdAsync(Guid roomId);
    Task AddUserToGroupAsync(string userId, Guid groupId);
    Task RemoveUserFromGroupAsync(string userId, Guid groupId);
    Task MoveUserToGroupAsync(string userId, Guid newGroupId);
    Task<Guid> GetGroupByUserIdAndRoomIdAsync(string userId, Guid roomId);
}