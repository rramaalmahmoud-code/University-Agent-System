using System.Threading;
using System.Threading.Tasks;
using University_Agent_System.Models.ViewModel;
using University_Agent_System.Services.Dashboard;

namespace University_Agent_System.Services.AgentStatistics
{
    public interface IAgentStatisticsService
    {
        Task<AgentStatisticsPageVM> BuildPageAsync(
            string search,
            string health,
            string semester,
            int page,
            int pageSize,
            DashboardLanguage language,
            CancellationToken cancellationToken = default);

        Task<AgentStatisticsExportResult> BuildExportAsync(
            string search,
            string health,
            string semester,
            DashboardLanguage language,
            CancellationToken cancellationToken = default);
    }

    public sealed class AgentStatisticsExportResult
    {
        public byte[] Content { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; } = "text/csv";
    }
}
