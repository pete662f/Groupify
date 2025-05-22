using Groupify.Models.Domain;

namespace Groupify.Data.Services.Interfaces;

public interface IInsightService
{
    Task CreateInsightProfileAsync(string userId, Insight insight);
    Task<Insight> GetInsightByUserIdAsync(string userId);
    Task UpdateInsightAsync(string userId, Insight insight);
    Task<bool> HasInsightProfileAsync(string userId);
}