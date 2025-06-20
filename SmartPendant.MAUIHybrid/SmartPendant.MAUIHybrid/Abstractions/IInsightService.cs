using SmartPendant.MAUIHybrid.Models;

namespace SmartPendant.MAUIHybrid.Abstractions
{
    public interface IInsightService
    {
        Task GenerateAndApplyInsightAsync(Conversation conversation, CancellationToken cancellationToken = default);
    }
}
