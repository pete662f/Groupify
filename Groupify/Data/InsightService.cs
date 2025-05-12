using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Groupify.Data;

public class InsightService
{
    private readonly GroupifyDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InsightService(GroupifyDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task CreateInsightProfileAsync(string userId, Insight insight)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        // Only students should have Insight profiles
        if (!await _userManager.IsInRoleAsync(user, "Student"))
            throw new InvalidOperationException("User is not a student");
        
        var existing = await _context.Insights.FindAsync(userId);
        if (existing != null)
            throw new InvalidOperationException("User is already an existing insight");

        // Used to sanitize the data since the user can change the values
        var newInsight = new Insight
        {
            ApplicationUserId = userId,
            ApplicationUser = user,
            Red =  insight.Red,
            Blue = insight.Blue,
            Green = insight.Green,
            Yellow = insight.Yellow,
            WheelPosition = insight.WheelPosition
        };
        
        _context.Insights.Add(newInsight);
        await _context.SaveChangesAsync();
    }

    public async Task<Insight> GetInsightByUserIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        var insight = await _context.Insights.FindAsync(userId);
        if (insight == null)
            throw new InvalidOperationException("Insight not found");
        
        return insight;
    }
    
    public async Task UpdateInsightAsync(string userId, Insight insight)
    {
        var user = await _context.Users
            .Include(u => u.Insight)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        if (user.Insight == null)
            throw new InvalidOperationException("User does not have an insight profile");
        
        user.Insight.Blue = insight.Blue;
        user.Insight.Green = insight.Green;
        user.Insight.Red = insight.Red;
        user.Insight.Yellow = insight.Yellow;
        user.Insight.WheelPosition = insight.WheelPosition;
        
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasInsightProfileAsync(string userId)
    {
        var insight = await _context.Insights.FindAsync(userId);
        Console.WriteLine("Insight: " + insight);
        if (insight == null)
            return false;
        return true;
    }
}