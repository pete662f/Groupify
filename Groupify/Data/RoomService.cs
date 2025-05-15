using System.Numerics;
using System.Security.Claims;
using Groupify.Models.Domain;
using Groupify.Models.DTO;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data;

public class RoomService
{
    private readonly GroupifyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoomService(GroupifyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    // TODO: Add checks for user roles in nearly all methods to ensure that only the right users can access the methods (Even tho there are checks in the controller)
    
    public async Task<IEnumerable<UserMatchDto>> GetSingleMatchsAsync(Guid roomId, string userId, int maxCount = 10)
    {
        var user = await _context.Users.Include(u => u.Insight)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        var room = await _context.Rooms
            .Include(r => r.Users)
            .ThenInclude(u => u.Insight)
            .FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) 
            throw new InvalidOperationException("Room not found");
        
        var userInsight = user.Insight;
        if (userInsight == null)
            throw new InvalidOperationException("User insight not found");
        
        var bestMatches = room.Users
            .Where(u => u.Id != userId && u.Insight != null)
            .Select(u => new UserMatchDto
            {
                User = u,
                MatchPercentage = GetMatchPercentage(userInsight.ToVector4(), u.Insight!.ToVector4())
            })
            .OrderByDescending(x => x.MatchPercentage)
            .Take(maxCount)
            .ToList();
        
        return bestMatches;
    }

    private float GetMatchPercentage(Vector4 userInsight, Vector4 otherUserInsight)
    {
        // The “ideal” vector we want otherUserInsight to match
        Vector4 ideal = Vector4.Subtract(new Vector4(6,6,6,6), userInsight);

        // How far otherUserInsight is from that ideal
        float distance = Vector4.Distance(ideal, otherUserInsight);

        // Normalize by ideal’s length and invert to get a % score
        float matchProcent = 100.0f / (1.0f + distance / ideal.Length());
        
        return matchProcent;
    }
    
    public async Task<Room> GetRoomByIdAsync(Guid roomId) 
    {
        var room = await _context.Rooms
            .Include(r => r.Users)   
            .Include(r => r.Groups)
            .ThenInclude(g => g.Users)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            throw new InvalidOperationException("Room not found");
        
        return room;
    }

    public async Task<IEnumerable<Room>> GetRoomsByUserIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        var rooms = await _context.Rooms
            .Where(r => r.Users.Contains(user))
            .ToListAsync();
        
        return rooms;
    }

    public async Task<IEnumerable<Room>> GetOwnedRoomsByUserIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        var rooms = await _context.Rooms
            .Where(r => r.OwnerId == userId)
            .ToListAsync();
        
        return rooms;
    }
    
    public async Task AddUserToRoomAsync(string userId, Guid roomId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        var room = await _context.Rooms
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == roomId);
        
        if (room == null)
            throw new InvalidOperationException("Room not found");
        
        if (room.Users.Contains(user))
            throw new InvalidOperationException("User already in room");
        
        room.Users.Add(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task RemoveUserFromRoomAsync(string userId, Guid roomId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        var room = await _context.Rooms
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null)
            throw new InvalidOperationException("Room not found");
        if (!room.Users.Contains(user))
            throw new InvalidOperationException("User not in room");
        
        var group = await _context.Groups
            .Include(g => g.Users)
            .FirstOrDefaultAsync(g => g.RoomId == roomId && g.Users.Contains(user));
        if (group != null)
        {
            group.Users.Remove(user);
            if (group.Users.Count == 0)
            {
                _context.Groups.Remove(group);
            }
        }
        
        room.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    public async Task ChangeRoomNameAsync(Guid roomId, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName.Length < 2)
            throw new ArgumentException("Room name must be at least 2 characters long");
            
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == roomId);
        
        if (room == null)
            throw new InvalidOperationException("Room not found");
        
        room.Name = newName;
        
        await _context.SaveChangesAsync();
    }
    
    public async Task<Guid> CreateRoomAsync(string roomName, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        
        Console.WriteLine("name:"+roomName);
        if (string.IsNullOrWhiteSpace(roomName) || roomName.Length < 2)
            throw new ArgumentException("Room name must be at least 2 characters long");
        
        var room = new Room
        {
            Name = roomName,
            OwnerId = userId,
            Owner = user,
        };
        
        _context.Rooms.Add(room);
        
        await _context.SaveChangesAsync();
        
        return room.Id;
    }
    
    public async Task RemoveRoomAsync(ClaimsPrincipal caller, Guid roomId, string userId)
    {
        var room = await _context.Rooms
            .Include(r => r.Groups)
            .ThenInclude(g => g.Users)
            .Include(r => r.Users)
            .Include(r => r.Owner)
            .ThenInclude(u => u.CreatedRooms)
            .FirstOrDefaultAsync(r => r.Id == roomId);
        
        if (room == null)
            throw new InvalidOperationException("Room not found");
        
        if (room.OwnerId != userId && !caller.IsInRole("Admin"))
            throw new InvalidOperationException("Only the owner can delete the room");
        
        // Remove all users from the groups
        foreach (var group in room.Groups)
        {
            group.Users.Clear();
        }
        
        // Remove all groups completely
        _context.Groups.RemoveRange(room.Groups);
        
        // Remove all users from the room
        room.Users.Clear();
        
        // Remove the room from the owner's created rooms
        room.Owner.CreatedRooms.Remove(room);
        
        // Remove the room itself
        _context.Rooms.Remove(room);
        
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Room>> GetAllRoomsAsync()
    {
        var rooms = await _context.Rooms.ToListAsync();
        
        return rooms;
    }
}