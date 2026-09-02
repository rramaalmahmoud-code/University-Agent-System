using System.Threading;
using System.Threading.Tasks;
using University_Agent_System.Models.ViewModel;
using University_Agent_System.Services.Dashboard;

namespace University_Agent_System.Services.Agents
{
    public enum AgentStatusUpdateResult
    {
        Updated,
        AgentNotFound,
        InvalidStatus
    }

    public interface IAgentManagementService
    {
        Task<AgentStatusUpdateResult> ToggleStatusAsync(
            int agentId,
            string requestedStatus,
            CancellationToken cancellationToken = default);

        Task<AgentViewModel> GetAgentInfoAsync(
            int agentId,
            DashboardLanguage language,
            CancellationToken cancellationToken = default);
    }
}
