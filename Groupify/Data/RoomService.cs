using Groupify.Models.Domain;
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
    
    public async Task<Room> GetRoomByIdAsync(Guid roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.Users)    // <— bring back the Users M:N nav‐prop
            .Include(r => r.Groups)   // <— if you also want Groups
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
    
    public async Task RemoveRoomAsync(Guid roomId, string userId)
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
        
        if (room.OwnerId != userId)
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
}