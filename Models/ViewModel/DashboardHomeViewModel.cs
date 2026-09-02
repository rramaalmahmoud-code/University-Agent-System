using System.Collections.Generic;

namespace University_Agent_System.Models.ViewModel
{
    public class DashboardHomeViewModel
    {
        public string SearchTerm { get; set; }
        public string IntakeFilter { get; set; } = "current";
        public int? CurrentIntakeId { get; set; }
        public int? PreviousIntakeId { get; set; }
        public int? SelectedAgentId { get; set; }
        public string SelectedAgentName { get; set; }

        public int TotalStudents { get; set; }
        public int TotalAgents { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int StudentConfirmedCount { get; set; }

        public int PendingTotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }

        public List<agent> Agents { get; set; } = new List<agent>();
        public List<StudentWithMajorVM> LatestStudents { get; set; } = new List<StudentWithMajorVM>();
        public List<StudentWithMajorVM> SelectedAgentStudents { get; set; } = new List<StudentWithMajorVM>();
        public List<StudentWithMajorVM> PendingStudents { get; set; } = new List<StudentWithMajorVM>();
        public List<AgentStatisticsVM> AgentStatistics { get; set; } = new List<AgentStatisticsVM>();
        public List<DashboardNotificationVM> Notifications { get; set; } = new List<DashboardNotificationVM>();
    }

    public class ApplicationStatusReportVM
    {
        public string IntakeFilter { get; set; } = "current";
        public int? CurrentIntakeId { get; set; }
        public int? PreviousIntakeId { get; set; }
        public int TotalStudents { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public List<StatusSummaryVM> Statuses { get; set; } = new List<StatusSummaryVM>();
    }

    public class StatusSummaryVM
    {
        public string StatusName { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class AgentPerformanceReportVM
    {
        public string SearchTerm { get; set; }
        public string HealthFilter { get; set; } = "all";
        public string IntakeFilter { get; set; } = "current";
        public int? CurrentIntakeId { get; set; }
        public int? PreviousIntakeId { get; set; }
        public List<AgentStatisticsVM> Agents { get; set; } = new List<AgentStatisticsVM>();
    }

    public class AgentStatisticsVM
    {
        public int? AgentId { get; set; }
        public int? AgentCode { get; set; }
        public string AgentName { get; set; }
        public int TotalStudents { get; set; }
        public int PendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int StudentConfirmedCount { get; set; }
        public decimal ApprovalRate { get; set; }
        public string HealthStatus { get; set; }
        public string HealthCssClass { get; set; }
        public string AccountStatus { get; set; }
        public string AccountStatusDisplay { get; set; }
        public string AccountStatusCssClass { get; set; }

        public DateTime? ContractEndDate { get; set; }
        public bool IsContractExpired { get; set; }
    }

    public class DashboardNotificationVM
    {
        public string Type { get; set; }
        public string CssClass { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int? AgentId { get; set; }
        public int Priority { get; set; } = 10;
    }
}
