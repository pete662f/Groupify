using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data;

public class GroupService
{
    private readonly GroupifyDbContext _context;

    public GroupService(GroupifyDbContext context)
    {
        _context = context;
    }

    public async Task CreateGroupsAsync(int roomId, int groupSize)
    {
        var room = await _context.Rooms
            .Include(r => r.Users)
            .Include(r => r.Groups)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            throw new InvalidOperationException("Room not found");

        if (groupSize <= 0)
            throw new ArgumentException("Group size must be greater than 0");

        if (room.Users.Count < groupSize)
            throw new InvalidOperationException("Not enough users in the room to create groups of this size");

        var users = room.Users.ToList();
        
        // TODO: Group users via a better algorithm
        for (int i = 0; i < users.Count; i += groupSize)
        {
            var group = new Group { Room = room };
            foreach (var user in users.Skip(i).Take(groupSize))
                group.Users.Add(user);
            room.Groups.Add(group);
        }

        await _context.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<Group>> GetGroupsByRoomIdAsync(int roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.Groups)
            .ThenInclude(g => g.Users)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            throw new InvalidOperationException("Room not found");

        return room.Groups;
    }
    
    public async Task<IEnumerable<Group>> GetGroupsByUserIdAsync(string userId)
    {
        var user = await _context.Users
            .Include(u => u.Groups)
            .ThenInclude(g => g.Room)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("User not found");

        return user.Groups;
    }
    
    public async Task RemoveGroupAsync(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Users)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new InvalidOperationException("Group not found");

        // Remove all users from the group
        foreach (var user in group.Users)
        {
            group.Users.Remove(user);
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddUserToGroupAsync(string userId, int groupId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var group = await _context.Groups
            .Include(g => g.Users)
            .Include(g => g.Room)
            .ThenInclude(r => r.Users)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        
        if (group == null)
            throw new InvalidOperationException("Group not found");
        
        if (!group.Room.Users.Contains(user))
            throw new InvalidOperationException("User not in room");

        if (group.Users.Contains(user))
            throw new InvalidOperationException("User already in group");

        group.Users.Add(user);
        await _context.SaveChangesAsync();
    }
    
    public async Task RemoveUserFromGroupAsync(string userId, int groupId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");

        var group = await _context.Groups
            .Include(g => g.Users)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new InvalidOperationException("Group not found");

        if (!group.Users.Contains(user))
            throw new InvalidOperationException("User not in group");

        group.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
}