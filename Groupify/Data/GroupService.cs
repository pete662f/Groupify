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
}