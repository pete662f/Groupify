using System.Numerics;
using Groupify.Data.Services.Interfaces;
using Groupify.Extensions;
using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data.Services;

public class GroupService : IGroupService
{
    private readonly GroupifyDbContext _context;

    public GroupService(GroupifyDbContext context)
    {
        _context = context;
    }
    
    // TODO: Add checks for user roles in nearly all methods to ensure that only the right users can access the methods (Even tho there are checks in the controller)
    
    // This method are used to calculate the average Insight energies of a list of users
    private Vector4 CalculateAverageInsightVector4(List<ApplicationUser> users)
    {
        if (users == null || users.Count == 0)
            throw new InvalidOperationException("Users list cannot be null or empty");

        Vector4 sum = Vector4.Zero;
        foreach (var user in users)
        {
            if (user.Insight == null)
                throw new InvalidOperationException("All users must have an Insight profile");
            
            sum = Vector4.Add(sum, user.Insight.ToVector4());
        }
        return Vector4.Divide(sum, users.Count());
    }
    
    private List<List<ApplicationUser>> SwapOptimization(List<List<ApplicationUser>> groups, Vector4 globalAverage, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            // Randomly select two groups
            int group1 = Random.Shared.Next(groups.Count);
            int group2;
            do
            {
                group2 = Random.Shared.Next(groups.Count);
            } while(group1 == group2);
            
            // Check if groups exist
            if (!groups[group1].Any() || !groups[group2].Any()) continue;
            
            // Check if groups are emptyz
            if (groups[group1].Count == 0 || groups[group2].Count == 0) continue;
            
            int index1 = Random.Shared.Next(groups[group1].Count-1);
            int index2 = Random.Shared.Next(groups[group2].Count-1);
            
            var user1 = groups[group1][index1];
            var user2 = groups[group2][index2];
            
            // Calculate the score before the swap
            float originalScore1 = Vector4Extensions.Sum(Vector4.Abs(Vector4.Subtract(CalculateAverageInsightVector4(groups[group1]), globalAverage)));
            float originalScore2 = Vector4Extensions.Sum(Vector4.Abs(Vector4.Subtract(CalculateAverageInsightVector4(groups[group2]), globalAverage)));
            
            // Swap users
            groups[group1][index1] = user2;
            groups[group2][index2] = user1;
            
            // Calculate the score after the swap
            float newScore1 = Vector4Extensions.Sum(Vector4.Abs(Vector4.Subtract(CalculateAverageInsightVector4(groups[group1]), globalAverage)));
            float newScore2 = Vector4Extensions.Sum(Vector4.Abs(Vector4.Subtract(CalculateAverageInsightVector4(groups[group2]), globalAverage)));
            
            // If the swap improved the score, keep it else swap back
            if (newScore1 + newScore2 >= originalScore1 + originalScore2)
            {
                // Swap back
                groups[group1][index1] = user1;
                groups[group2][index2] = user2;
            }
        }
        
        return groups;
    }

    private List<List<ApplicationUser>> GreedyGrouping(List<ApplicationUser> users, List<List<ApplicationUser>> groups, Vector4 globalAverage, int groupSize)
    {
        
        
        foreach (ApplicationUser user in users)
        {
            int bestGroup = -1; // -1 means no group found
            int bestScore = int.MaxValue;
            
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].Count >= groupSize) continue;

                // Make a copy of the group and add the user to it (this is slightly inefficient)
                var tempGroup = new List<ApplicationUser>(groups[i]) { user };
                Vector4 average = CalculateAverageInsightVector4(tempGroup);
                Vector4 difference = Vector4.Abs(Vector4.Add(average, globalAverage));
                int score = (int)Vector4Extensions.Sum(difference); 
                
                // Lower score is better
                if (score >= bestScore) continue;
                
                // Found a better group
                bestGroup = i;
                bestScore = score;
                
            }

            groups[bestGroup].Add(user);
        }

        return groups;
    }
    
    public async Task<Vector4> GroupInsightAsync(Guid groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Users)
            .ThenInclude(u => u.Insight)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new InvalidOperationException("Group not found");

        return CalculateAverageInsightVector4(group.Users.ToList());
    }
    
    public async Task<Group> GetGroupByIdAsync(Guid groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Users)
            .ThenInclude(u => u.Insight)
            .Include(g => g.Room)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new InvalidOperationException("Group not found");

        return group;
    }

    public async Task CreateGroupsAsync(Guid roomId, int groupSize)
    {
        var room = await _context.Rooms
            .Include(r => r.Users)
            .ThenInclude(u => u.Insight)
            .Include(r => r.Groups)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            throw new InvalidOperationException("Room not found");
        
        if (room.Groups.Any())
            throw new InvalidOperationException("Groups already exist in this room");

        if (groupSize < 2)
            throw new ArgumentException("Group size must be 2 or more");
        
        // Minimum is one full group plus at least 2 people for another group
        if (room.Users.Count < groupSize + 2)
            throw new InvalidOperationException($"Need at least {groupSize + 2} users to create meaningful groups (one of {groupSize} people and one of at least 2 people)");
        
        if (room.Users.Any(u => u.Insight == null))
            throw new InvalidOperationException("All users must have an Insight profile to create groups");
        
        // Begin creating groups //
        var users = room.Users.ToList();
        
        Vector4 globalAverage = CalculateAverageInsightVector4(users);
        
        // Sort based on total Insight values
        users.Sort((u1, u2) =>
        {
            float total1 = Vector4Extensions.Sum(u1.Insight!.ToVector4());
            float total2 = Vector4Extensions.Sum(u2.Insight!.ToVector4());
            return total1.CompareTo(total2);
        });
        
        int totalGroups = (int)Math.Ceiling((double)users.Count / groupSize);
        var groups = new List<List<ApplicationUser>>(totalGroups);
        
        // Initialize groups
        for (int i = 0; i < totalGroups; i++)
        {
            groups.Add(new List<ApplicationUser>());
        }
        
        // Greedy grouping
        groups = GreedyGrouping(users, groups, globalAverage, groupSize);
        
        // Swap users to balance groups
        groups = SwapOptimization(groups, globalAverage, iterations:users.Count*10);
        
        // Create group entities
        int groupIndex = 0;
        foreach (List<ApplicationUser> group in groups)
        {
            var newGroup = new Group
            {
                GroupNumber = groupIndex+1, // 1-based index
                RoomId = roomId,
                Users = group.ToList()
            };
            
            _context.Groups.Add(newGroup);
            room.Groups.Add(newGroup);
            
            foreach (var user in group)
            {
                user.Groups.Add(newGroup);
            }

            groupIndex++;
        }
        
        await _context.SaveChangesAsync();
    }
    
    public async Task<IEnumerable<Group>> GetGroupsByRoomIdAsync(Guid roomId)
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
    
    public async Task RemoveGroupAsync(Guid groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Users)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            throw new InvalidOperationException("Group not found");

        // Remove all users from the group
        group.Users.Clear();

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
    }
    
    public async Task RemoveAllGroupsByRoomIdAsync(Guid roomId)
    {
        var room = await _context.Rooms
            .Include(r => r.Groups)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            throw new InvalidOperationException("Room not found");

        // Clear users from all groups
        foreach (var group in room.Groups)
        {
            group.Users.Clear();
        }
        
        // Remove all groups
        _context.Groups.RemoveRange(room.Groups);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddUserToGroupAsync(string userId, Guid groupId)
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
    
    public async Task RemoveUserFromGroupAsync(string userId, Guid groupId)
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

    public async Task MoveUserToGroupAsync(string userId, Guid newGroupId)
    {
        // Load the user + their existing groups
        var user = await _context.Users
            .Include(u => u.Groups)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new InvalidOperationException($"User not found.");

        // Load the target group
        var newGroup = await _context.Groups
            .FirstOrDefaultAsync(g => g.Id == newGroupId);
        if (newGroup == null)
            throw new InvalidOperationException($"Group not found.");
        
        // Find any old group in the same Room
        var oldGroup = user.Groups
            .FirstOrDefault(g => g.RoomId == newGroup.RoomId);
        if (oldGroup != null)
            user.Groups.Remove(oldGroup);

        user.Groups.Add(newGroup);

        await _context.SaveChangesAsync();
    }

    public async Task<Guid> GetGroupByUserIdAndRoomIdAsync(string userId, Guid roomId)
    {
        var user = await _context.Users
            .Include(u => u.Groups)
            .ThenInclude(g => g.Room)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) 
            throw new InvalidOperationException("User not found.");
        
        var group = user.Groups
            .FirstOrDefault(g => g.RoomId == roomId);
        if (group == null) 
            return Guid.Empty;
        
        return group.Id;
    }
}