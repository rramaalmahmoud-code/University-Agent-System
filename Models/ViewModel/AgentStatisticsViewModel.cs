using System;
using System.Collections.Generic;

namespace University_Agent_System.Models.ViewModel
{
    public class AgentStatisticsPageVM
    {
        public string SearchTerm { get; set; }
        public string HealthFilter { get; set; } = "all";
        public string IntakeFilter { get; set; } = "current";
        public int? CurrentIntakeId { get; set; }
        public int? PreviousIntakeId { get; set; }

        public int TotalAgents { get; set; }
        public int ActiveAgents { get; set; }
        public int InactiveAgents { get; set; }
        public int TotalStudents { get; set; }
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int AgentsNeedAttention { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalFilteredAgents { get; set; }
        public int TotalPages { get; set; }

        public int CriticalNotifications { get; set; }
        public int WarningNotifications { get; set; }
        public string NotificationFingerprint { get; set; }

        public List<AgentStatisticsRowVM> Agents { get; set; } = new List<AgentStatisticsRowVM>();
        public List<AgentStatisticsNotificationVM> Notifications { get; set; } = new List<AgentStatisticsNotificationVM>();
        public List<AgentStatisticsAgentOptionVM> AgentOptions { get; set; }
    = new List<AgentStatisticsAgentOptionVM>();
    }

    public class AgentStatisticsRowVM
    {
        public int AgentId { get; set; }
        public int? AgentCode { get; set; }
        public string AgentName { get; set; }
        public string AgentEmail { get; set; }
        public string AgentCity { get; set; }
        public int? CountryId { get; set; }
        public bool IsActive { get; set; }

        public int TotalStudents { get; set; }
        public int NewCount { get; set; }
        public int PendingCount { get; set; }
        public int OldPendingCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int MissingDocumentsCount { get; set; }
        public decimal ApprovalRate { get; set; }

        public DateTime? LastActivityAt { get; set; }
        public int? DaysSinceLastActivity { get; set; }
        public string HealthStatus { get; set; }
        public string HealthFilterValue { get; set; }
        public string HealthCssClass { get; set; }
        public string AccountStatus { get; set; }
        public string AccountStatusDisplay { get; set; }
        public string AccountStatusCssClass { get; set; }

        public DateTime? ContractEndDate { get; set; }
        public bool IsContractExpired { get; set; }
    }

    public class AgentStatisticsNotificationVM
    {
        public string Key { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; }
        public string Severity { get; set; }
        public string Icon { get; set; }
        public string IconCssClass { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}
