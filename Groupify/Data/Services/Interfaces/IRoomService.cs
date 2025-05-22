using System.Security.Claims;
using Groupify.Models.Domain;
using Groupify.Models.DTO;

namespace Groupify.Data.Services.Interfaces;

public interface IRoomService
{
    Task<IEnumerable<UserMatchDto>> GetSingleMatchesAsync(Guid roomId, string userId, int maxCount = 10);
    Task<Room> GetRoomByIdAsync(Guid roomId);
    Task<IEnumerable<Room>> GetRoomsByUserIdAsync(string userId);
    Task<IEnumerable<Room>> GetOwnedRoomsByUserIdAsync(string userId);
    Task AddUserToRoomAsync(string userId, Guid roomId);
    Task RemoveUserFromRoomAsync(string userId, Guid roomId);
    Task ChangeRoomNameAsync(Guid roomId, string newName);
    Task<Guid> CreateRoomAsync(string roomName, string userId);
    Task RemoveRoomAsync(ClaimsPrincipal caller, Guid roomId, string userId);
    Task<IEnumerable<Room>> GetAllRoomsAsync();
}