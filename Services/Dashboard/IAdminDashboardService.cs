using System.Threading;
using System.Threading.Tasks;
using University_Agent_System.Models.ViewModel;

namespace University_Agent_System.Services.Dashboard
{
    public interface IAdminDashboardService
    {
        Task<DashboardHomeViewModel> BuildHomeAsync(
            DashboardHomeRequest request,
            DashboardLanguage language,
            CancellationToken cancellationToken = default);

        Task<ApplicationStatusReportVM> BuildApplicationStatusReportAsync(
            string semester,
            DashboardLanguage language,
            CancellationToken cancellationToken = default);

        Task<AgentPerformanceReportVM> BuildAgentPerformanceReportAsync(
            string search,
            string semester,
            string health,
            DashboardLanguage language,
            CancellationToken cancellationToken = default);
    }
}
