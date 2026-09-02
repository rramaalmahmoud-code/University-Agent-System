using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using University_Agent_System.Services.Agents;
using University_Agent_System.Services.AgentStatistics;
using University_Agent_System.Services.Dashboard;

namespace University_Agent_System.Controllers
{
    public abstract class AdminDashboardControllerBase : Controller
    {
        private readonly IAdminDashboardService _dashboardService;
        private readonly IAgentManagementService _agentManagementService;
        private readonly IAgentStatisticsService _agentStatisticsService;

        protected AdminDashboardControllerBase(
            IAdminDashboardService dashboardService,
            IAgentManagementService agentManagementService,
            IAgentStatisticsService agentStatisticsService)
        {
            _dashboardService = dashboardService;
            _agentManagementService = agentManagementService;
            _agentStatisticsService = agentStatisticsService;
        }

        protected async Task<IActionResult> HomeCoreAsync(
            string search, string semester, int page, int pageSize,
            DashboardLanguage language, string viewPath)
        {
            var model = await _dashboardService.BuildHomeAsync(
                new DashboardHomeRequest
                {
                    Search = search,
                    Semester = semester,
                    Page = page,
                    PageSize = pageSize
                },
                language,
                HttpContext.RequestAborted);
            return View(viewPath, model);
        }

        protected async Task<IActionResult> ApplicationStatusReportCoreAsync(
            string semester, DashboardLanguage language, string viewPath)
        {
            var model = await _dashboardService.BuildApplicationStatusReportAsync(
                semester, language, HttpContext.RequestAborted);
            return View(viewPath, model);
        }

        protected async Task<IActionResult> AgentPerformanceReportCoreAsync(
            string search, string semester, string health,
            DashboardLanguage language, string viewPath)
        {
            var model = await _dashboardService.BuildAgentPerformanceReportAsync(
                search, semester, health, language, HttpContext.RequestAborted);
            return View(viewPath, model);
        }

        protected async Task<IActionResult> UpdateAgentStatusCoreAsync(
            int agentId, string status, string invalidStatusMessage)
        {
            var result = await _agentManagementService.ToggleStatusAsync(
                agentId, status, HttpContext.RequestAborted);

            if (result == AgentStatusUpdateResult.InvalidStatus)
                return BadRequest(invalidStatusMessage);
            if (result == AgentStatusUpdateResult.AgentNotFound)
                return NotFound();

            return RedirectToAction("AgentInfo", new { agentId });
        }

        protected async Task<IActionResult> AgentInfoCoreAsync(
            int agentId, DashboardLanguage language, string viewPath)
        {
            var model = await _agentManagementService.GetAgentInfoAsync(
                agentId, language, HttpContext.RequestAborted);

            if (model == null)
                return NotFound();

            return View(viewPath, model);
        }

        protected async Task<IActionResult> AgentStatisticsCoreAsync(
            string search,
            string health,
            string semester,
            int page,
            int pageSize,
            DashboardLanguage language,
            string viewPath)
        {
            var model = await _agentStatisticsService.BuildPageAsync(
                search,
                health,
                semester,
                page,
                pageSize,
                language,
                HttpContext.RequestAborted);
            return View(viewPath, model);
        }

        protected async Task<IActionResult> ExportAgentStatisticsCoreAsync(
            string search,
            string health,
            string semester,
            DashboardLanguage language)
        {
            var export = await _agentStatisticsService.BuildExportAsync(
                search,
                health,
                semester,
                language,
                HttpContext.RequestAborted);
            return File(export.Content, export.ContentType, export.FileName);
        }
    }
}
