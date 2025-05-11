using Groupify.Models.Domain;
using Groupify.Models.Identity;
using Microsoft.AspNetCore.Identity;

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

    public async Task CreateInsightProfileAsync(string userId)
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

        var insight = new Insight
        {
            ApplicationUserId = userId,
            ApplicationUser = user,
            Red =  0,
            Blue = 0,
            Green = 0,
            Yellow = 0,
            WheelPosition = 0
        };
        
        _context.Insights.Add(insight);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateInsightAsync(string userId, Insight insight)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new InvalidOperationException("User not found");
        
        user.Insight.Blue = insight.Blue;
        user.Insight.Green = insight.Green;
        user.Insight.Red = insight.Red;
        user.Insight.Yellow = insight.Yellow;
        user.Insight.WheelPosition = insight.WheelPosition;
        
        await _context.SaveChangesAsync();
    }
}