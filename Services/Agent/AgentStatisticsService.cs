using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using University_Agent_System.Models.ViewModel;
using University_Agent_System.Services.Dashboard;

namespace University_Agent_System.Services.AgentStatistics
{
    public sealed class AgentStatisticsService : IAgentStatisticsService
    {
        private readonly IDbConnection _db;

        public AgentStatisticsService(IDbConnection db)
        {
            _db = db;
        }

        private static string GetAccountStatus(AgentRow agent)
        {
            var status = (agent.AgentStatus ?? string.Empty)
                .Replace("\u00A0", " ")
                .Trim()
                .ToLowerInvariant();

            if (status == "blocked" ||
                status == "block")
            {
                return "Blocked";
            }

            if (status == "freezed" ||
                status == "frozen" ||
                status == "freeze" ||
                status == "frezed")
            {
                return "Freezed";
            }

            if (agent.ContractEndDate.HasValue &&
                agent.ContractEndDate.Value.Date < DateTime.Today)
            {
                return "Expired";
            }

            if (agent.Active != 1)
            {
                return "Inactive";
            }

            return "Active";
        }
        private static string GetAccountStatusDisplay(
            string status,
            DashboardLanguage language)
        {
            if (language == DashboardLanguage.English)
            {
                return status switch
                {
                    "Blocked" => "Blocked",
                    "Freezed" => "Frozen",
                    "Expired" => "Contract Expired",
                    "Inactive" => "Inactive",
                    _ => "Active"
                };
            }

            return status switch
            {
                "Blocked" => "محظور",
                "Freezed" => "مجمّد",
                "Expired" => "العقد منتهي",
                "Inactive" => "غير نشط",
                _ => "نشط"
            };
        }

        private static string GetAccountStatusCssClass(string status)
        {
            return status switch
            {
                "Blocked" => "account-blocked",
                "Freezed" => "account-freezed",
                "Expired" => "account-expired",
                "Inactive" => "account-inactive",
                _ => "account-active"
            };
        }
        public async Task<AgentStatisticsPageVM> BuildPageAsync(
            string search,
            string health,
            string semester,
            int page,
            int pageSize,
            DashboardLanguage language,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Min(Math.Max(pageSize, 5), 100);

            var data = await LoadDataAsync(semester, cancellationToken);
            var allRows = BuildRows(data.Agents, data.Students, language);
            var notifications = BuildNotifications(allRows, language);
            var filteredRows = ApplyFilters(allRows, search, health);
            var totalFiltered = filteredRows.Count;
            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalFiltered / (double)pageSize));
            page = Math.Min(page, totalPages);

            return new AgentStatisticsPageVM
            {
                SearchTerm = search,
                HealthFilter = NormalizeHealthFilter(health),
                IntakeFilter = data.SemesterFilter,
                CurrentIntakeId = data.CurrentSemesterId,
                PreviousIntakeId = data.PreviousSemesterId,
                TotalAgents = allRows.Count,
                ActiveAgents = allRows.Count(x => x.IsActive),
                InactiveAgents = allRows.Count(x => !x.IsActive),
                TotalStudents = allRows.Sum(x => x.TotalStudents),
                TotalPending = allRows.Sum(x => x.PendingCount),
                TotalApproved = allRows.Sum(x => x.ApprovedCount),
                AgentsNeedAttention = allRows.Count(x => x.HealthFilterValue != "healthy"),
                CurrentPage = page,
                PageSize = pageSize,
                TotalFilteredAgents = totalFiltered,
                TotalPages = totalPages,
                Agents = filteredRows
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                AgentOptions = allRows
    .OrderBy(x => x.AgentName)
    .Select(x => new AgentStatisticsAgentOptionVM
    {
        AgentId = x.AgentId,
        AgentCode = x.AgentCode,
        AgentName = x.AgentName,
        AccountStatus = x.AccountStatus,
        IsContractExpired = x.IsContractExpired
    })
    .ToList(),
                Notifications = notifications,
                CriticalNotifications = notifications.Count(x => x.Severity == "Critical"),
                WarningNotifications = notifications.Count(x => x.Severity == "Warning"),
                NotificationFingerprint = string.Join("|", notifications.Select(x => x.Key))

            };
        }

        public async Task<AgentStatisticsExportResult> BuildExportAsync(
            string search,
            string health,
            string semester,
            DashboardLanguage language,
            CancellationToken cancellationToken = default)
        {
            var data = await LoadDataAsync(semester, cancellationToken);
            var rows = ApplyFilters(
                BuildRows(data.Agents, data.Students, language),
                search,
                health);

            var csv = new StringBuilder();
            csv.AppendLine(language == DashboardLanguage.Arabic
                ? "الوكيل,الكود,البريد الإلكتروني,المدينة,إجمالي الطلاب,جديد,معلق,معلق منذ أكثر من 3 أيام,قيد المراجعة,مقبول,مرفوض,وثائق ناقصة,نسبة القبول,آخر نشاط,حالة الأداء"
                : "Agent,Code,Email,City,Total Students,New,Pending,Old Pending,Under Review,Approved,Rejected,Missing Documents,Approval Rate,Last Activity,Health");

            foreach (var item in rows)
            {

                csv.AppendLine(string.Join(",", new[]
                {
                    CsvValue(item.AgentName),
                    CsvValue(Convert.ToString(item.AgentCode)),
                    CsvValue(item.AgentEmail),
                    CsvValue(item.AgentCity),
                    item.TotalStudents.ToString(),
                    item.NewCount.ToString(),
                    item.PendingCount.ToString(),
                    item.OldPendingCount.ToString(),
                    item.UnderReviewCount.ToString(),
                    item.ApprovedCount.ToString(),
                    item.RejectedCount.ToString(),
                    item.MissingDocumentsCount.ToString(),
                    item.ApprovalRate.ToString("0.0") + "%",
                    CsvValue(item.LastActivityAt?.ToString("yyyy-MM-dd HH:mm")),
                    CsvValue(language == DashboardLanguage.Arabic
                        ? ArabicHealth(item.HealthStatus)
                        : item.HealthStatus)
                }));
            }

            return new AgentStatisticsExportResult
            {
                Content = Encoding.UTF8.GetBytes("\uFEFF" + csv),
                FileName = language == DashboardLanguage.Arabic
                    ? $"agent-statistics-ar-{DateTime.Now:yyyyMMdd-HHmm}.csv"
                    : $"agent-statistics-{DateTime.Now:yyyyMMdd-HHmm}.csv"
            };
        }

        private async Task<StatisticsData> LoadDataAsync(
            string semester,
            CancellationToken cancellationToken)
        {
            var semesterRows = (await _db.QueryAsync<SemesterRow>(
                new CommandDefinition(@"
SELECT TOP (2) semesterId AS SemesterId
FROM Students
WHERE active = 1 AND semesterId IS NOT NULL
GROUP BY semesterId
ORDER BY semesterId DESC",
                    cancellationToken: cancellationToken))).ToList();

            int? currentSemesterId = semesterRows.Count > 0
                ? semesterRows[0].SemesterId
                : (int?)null;
            int? previousSemesterId = semesterRows.Count > 1
                ? semesterRows[1].SemesterId
                : (int?)null;
            var normalizedSemester = NormalizeSemester(semester);
            int? selectedSemesterId = normalizedSemester == "all"
                ? null
                : normalizedSemester == "previous"
                    ? previousSemesterId
                    : currentSemesterId;

            var students = (await _db.QueryAsync<StudentRow>(
                new CommandDefinition(@"
SELECT
    s.studentId AS StudentId,
    s.agentId AS AgentId,
    s.statusId AS StatusId,
    s.semesterId AS SemesterId,
    s.CreatedAt,
    s.studentProof_of_Identity AS StudentProofOfIdentity,
    s.studentHigh_School_Certificate AS StudentHighSchoolCertificate,
    s.studentHigh_School_Certificate2 AS StudentHighSchoolCertificate2,
    s.studentHigh_School_Certificate3 AS StudentHighSchoolCertificate3,
    s.studentHigh_School_Certificate4 AS StudentHighSchoolCertificate4,
    s.studentHigh_School_Certificate5 AS StudentHighSchoolCertificate5,
    s.studentGrades_Report AS StudentGradesReport,
    s.studentGrades_Report1 AS StudentGradesReport1,
    st.statusEnglish AS StatusEnglish
FROM Students s
LEFT JOIN Statuses st ON st.statusId = s.statusId AND st.active = 1
WHERE s.active = 1
  AND (@SemesterId IS NULL OR s.semesterId = @SemesterId)",
                    new { SemesterId = selectedSemesterId },
                    cancellationToken: cancellationToken))).ToList();

            var agents = (await _db.QueryAsync<AgentRow>(
               new CommandDefinition(@"
SELECT
    agentId AS AgentId,
    agentCode AS AgentCode,
    agentNameArabic AS AgentNameArabic,
    agentNameEnglish AS AgentNameEnglish,
    agentEmail AS AgentEmail,
    city AS City,
    countryId AS CountryId,
    active AS Active,
    agentStatus AS AgentStatus,
    contractEndDate AS ContractEndDate
FROM Agents
WHERE active = 1 OR active = 0
ORDER BY agentNameEnglish",
                   cancellationToken: cancellationToken))).ToList();

            return new StatisticsData
            {
                SemesterFilter = normalizedSemester,
                CurrentSemesterId = currentSemesterId,
                PreviousSemesterId = previousSemesterId,
                Students = students,
                Agents = agents
            };
        }

        private static List<AgentStatisticsRowVM> BuildRows(
            List<AgentRow> agents,
            List<StudentRow> students,
            DashboardLanguage language)
        {
            var now = DateTime.Now;
            var newFrom = now.AddDays(-7);
            var oldPendingBefore = now.AddDays(-3);
            var studentsByAgent = students
                .Where(x => x.AgentId.HasValue)
                .GroupBy(x => x.AgentId.Value)
                .ToDictionary(x => x.Key, x => x.ToList());
            var result = new List<AgentStatisticsRowVM>(agents.Count);

            foreach (var agent in agents)
            {
                if (!studentsByAgent.TryGetValue(agent.AgentId, out var rows))
                    rows = new List<StudentRow>();

                var pending = rows.Where(IsPending).ToList();
                var approved = rows.Count(IsApproved);
                var rejected = rows.Count(IsRejected);
                var decided = approved + rejected;
                var rejectionRate = decided == 0 ? 0m : rejected * 100m / decided;
                var lastActivity = rows.Count == 0
                    ? (DateTime?)null
                    : rows.Max(x => x.CreatedAt);
                var daysSinceActivity = lastActivity.HasValue
                    ? Math.Max(0, (int)(now - lastActivity.Value).TotalDays)
                    : (int?)null;
                var missingDocuments = rows.Count(HasMissingDocuments);
                var oldPending = pending.Count(x => x.CreatedAt < oldPendingBefore);

                var healthStatus = "Healthy";
                var healthFilter = "healthy";
                var healthClass = "healthy";
                var accountStatus = GetAccountStatus(agent);

                var isContractExpired =
                    agent.ContractEndDate.HasValue &&
                    agent.ContractEndDate.Value.Date < now.Date;

                var isOperational =
                    agent.Active == 1 &&
                    accountStatus != "Blocked" &&
                    accountStatus != "Freezed" &&
                    accountStatus != "Expired";
                if (!isOperational || !lastActivity.HasValue)
                {
                    healthStatus = "Inactive";
                    healthFilter = "inactive";
                    healthClass = "inactive";
                }
                else if ((daysSinceActivity.HasValue && daysSinceActivity.Value >= 14) ||
                         pending.Count >= 25 || rejectionRate >= 40m ||
                         missingDocuments >= 10)
                {
                    healthStatus = "Critical";
                    healthFilter = "critical";
                    healthClass = "critical";
                }
                else if ((daysSinceActivity.HasValue && daysSinceActivity.Value >= 7) ||
                         pending.Count >= 15 || oldPending > 0 ||
                         rejectionRate >= 25m || missingDocuments >= 5)
                {
                    healthStatus = "Needs Attention";
                    healthFilter = "attention";
                    healthClass = "warning";
                }
           
                result.Add(new AgentStatisticsRowVM
                {
                    AgentId = agent.AgentId,
                    AgentCode = agent.AgentCode,
                    AgentName = GetAgentName(agent, language),
                    AgentEmail = agent.AgentEmail,
                    AgentCity = agent.City,
                    CountryId = agent.CountryId,
                    IsActive = isOperational,

                    AccountStatus = accountStatus,

                    AccountStatusDisplay =
    GetAccountStatusDisplay(accountStatus, language),

                    AccountStatusCssClass =
    GetAccountStatusCssClass(accountStatus),

                    ContractEndDate = agent.ContractEndDate,

                    IsContractExpired = isContractExpired,
                    TotalStudents = rows.Count,
                    NewCount = rows.Count(x => x.CreatedAt >= newFrom),
                    PendingCount = pending.Count,
                    OldPendingCount = oldPending,
                    UnderReviewCount = rows.Count(IsUnderReview),
                    ApprovedCount = approved,
                    RejectedCount = rejected,
                    MissingDocumentsCount = missingDocuments,
                    ApprovalRate = decided == 0
                        ? 0m
                        : Math.Round(approved * 100m / decided, 1),
                    LastActivityAt = lastActivity,
                    DaysSinceLastActivity = daysSinceActivity,
                    HealthStatus = healthStatus,
                    HealthFilterValue = healthFilter,
                    HealthCssClass = healthClass
                });
            }

            return result
                .OrderByDescending(x => HealthPriority(x.HealthFilterValue))
                .ThenByDescending(x => x.PendingCount)
                .ThenBy(x => x.AgentName)
                .ToList();
        }

        private static List<AgentStatisticsNotificationVM> BuildNotifications(
            List<AgentStatisticsRowVM> rows,
            DashboardLanguage language)
        {
            var notifications = new List<AgentStatisticsNotificationVM>();
            var arabic = language == DashboardLanguage.Arabic;

            foreach (var item in rows)
            {

                if (item.IsContractExpired)
                {
                    var contractEndDate = item.ContractEndDate?.ToString("dd/MM/yyyy");

                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"expired-contract-{item.AgentId}-{item.ContractEndDate:yyyyMMdd}",

                        AgentId = item.AgentId,
                        AgentName = item.AgentName,

                        Severity = "Critical",

                        Icon = "fas fa-file-contract",
                        IconCssClass = "gold",

                        Title = arabic
                            ? $"انتهى عقد الوكيل {item.AgentName}"
                            : $"{item.AgentName}'s contract has expired",

                        Message = arabic
                            ? $"انتهى عقد الوكيل بتاريخ {contractEndDate ?? "غير محدد"}. يجب مراجعة العقد وحالة حساب الوكيل."
                            : $"The agent contract expired on {contractEndDate ?? "N/A"}. The contract and account status require review."
                    });
                }
                if (item.AccountStatus == "Blocked")
{
    notifications.Add(new AgentStatisticsNotificationVM
    {
        Key = $"blocked-agent-{item.AgentId}",

        AgentId = item.AgentId,
        AgentName = item.AgentName,

        Severity = "Critical",

        Icon = "fas fa-ban",
        IconCssClass = "",

        Title = arabic
            ? $"الوكيل {item.AgentName} محظور"
            : $"{item.AgentName} is blocked",

        Message = arabic
            ? "حساب الوكيل محظور ولا يستطيع الدخول إلى النظام. يجب على الإدارة مراجعة حالة الحساب."
            : "The agent account is blocked and cannot access the system. The account status requires admin review."
    });
}

                var normalizedAccountStatus =
     (item.AccountStatus ?? string.Empty)
     .Replace("\u00A0", " ")
     .Trim()
     .ToLowerInvariant();

                var isFrozen =
                    normalizedAccountStatus == "freezed" ||
                    normalizedAccountStatus == "frozen" ||
                    normalizedAccountStatus == "freeze" ||
                    normalizedAccountStatus == "frezed";

                if (isFrozen)
                {
                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"freezed-agent-{item.AgentId}",
                        AgentId = item.AgentId,
                        AgentName = item.AgentName,

                        Severity = "Warning",
                        Icon = "fas fa-snowflake",
                        IconCssClass = "blue",

                        Title = arabic
                            ? $"حساب الوكيل {item.AgentName} مجمّد"
                            : $"{item.AgentName}'s account is frozen",

                        Message = arabic
                            ? "حساب الوكيل مجمّد مؤقتًا ولا يستطيع الدخول إلى النظام."
                            : "The agent account is temporarily frozen and cannot access the system."
                    });
                }
                if (item.PendingCount >= 15 || item.OldPendingCount > 0)
                {
                    var critical = item.PendingCount >= 25 || item.OldPendingCount >= 5;
                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"pending-{item.AgentId}-{item.PendingCount}-{item.OldPendingCount}",
                        AgentId = item.AgentId,
                        AgentName = item.AgentName,
                        Severity = critical ? "Critical" : "Warning",
                        Icon = "fas fa-hourglass-half",
                        IconCssClass = critical ? "" : "gold",
                        Title = arabic
                            ? $"لدى {item.AgentName} عدد {item.PendingCount} من الطلاب المعلقين"
                            : $"{item.AgentName} has {item.PendingCount} pending students",
                        Message = item.OldPendingCount > 0
                            ? arabic
                                ? $"يوجد {item.OldPendingCount} طلبًا معلقًا منذ أكثر من 3 أيام."
                                : $"{item.OldPendingCount} applications have been pending for more than 3 days."
                            : arabic
                                ? "وصل عدد الطلبات المعلقة إلى حد التنبيه."
                                : "The pending applications threshold has been reached."
                    });
                }

                var decisions = item.ApprovedCount + item.RejectedCount;
                var rejectionRate = decisions == 0
                    ? 0m
                    : item.RejectedCount * 100m / decisions;
                if (decisions >= 5 && rejectionRate >= 25m)
                {
                    var critical = rejectionRate >= 40m;
                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"rejection-{item.AgentId}-{rejectionRate:0.0}",
                        AgentId = item.AgentId,
                        AgentName = item.AgentName,
                        Severity = critical ? "Critical" : "Warning",
                        Icon = "fas fa-chart-line",
                        IconCssClass = critical ? "" : "gold",
                        Title = arabic
                            ? $"نسبة الرفض لدى {item.AgentName} مرتفعة"
                            : $"{item.AgentName} rejection rate is high",
                        Message = arabic
                            ? $"وصلت نسبة الرفض إلى {rejectionRate:0.0}%، وهي أعلى من حد 25%."
                            : $"Rejection rate reached {rejectionRate:0.0}%, above the 25% threshold."
                    });
                }

                if (item.MissingDocumentsCount >= 5)
                {
                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"documents-{item.AgentId}-{item.MissingDocumentsCount}",
                        AgentId = item.AgentId,
                        AgentName = item.AgentName,
                        Severity = item.MissingDocumentsCount >= 10 ? "Critical" : "Warning",
                        Icon = "fas fa-file-circle-exclamation",
                        IconCssClass = "gold",
                        Title = arabic
                            ? $"لدى {item.AgentName} عدد {item.MissingDocumentsCount} من الملفات غير المكتملة"
                            : $"{item.AgentName} has {item.MissingDocumentsCount} incomplete files",
                        Message = arabic
                            ? "وثيقة الهوية أو شهادة الثانوية أو كشف العلامات المطلوب غير مرفق."
                            : "Required identity, high-school certificate or grades report is missing."
                    });
                }

                if (item.IsActive && item.DaysSinceLastActivity.HasValue &&
                    item.DaysSinceLastActivity.Value >= 7)
                {
                    var critical = item.DaysSinceLastActivity.Value >= 14;
                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"activity-{item.AgentId}-{item.DaysSinceLastActivity.Value}",
                        AgentId = item.AgentId,
                        AgentName = item.AgentName,
                        Severity = critical ? "Critical" : "Warning",
                        Icon = "fas fa-user-clock",
                        IconCssClass = critical ? "" : "blue",
                        Title = arabic
                            ? $"لا يوجد نشاط حديث للوكيل {item.AgentName}"
                            : $"{item.AgentName} has no recent activity",
                        Message = arabic
                            ? $"لم يتم تسجيل أي نشاط طلاب جديد منذ {item.DaysSinceLastActivity.Value} يومًا."
                            : $"No new student activity has been recorded for {item.DaysSinceLastActivity.Value} days."
                    });
                }
                else if (
                    item.AccountStatus != "Blocked" &&
                    item.AccountStatus != "Freezed" &&
                    item.AccountStatus != "Expired" &&
                    (!item.IsActive || !item.LastActivityAt.HasValue))
                {
                    notifications.Add(new AgentStatisticsNotificationVM
                    {
                        Key = $"inactive-{item.AgentId}-{item.IsActive}",
                        AgentId = item.AgentId,
                        AgentName = item.AgentName,
                        Severity = item.IsActive ? "Critical" : "Warning",
                        Icon = "fas fa-user-slash",
                        IconCssClass = "blue",
                        Title = arabic
                            ? item.IsActive
                                ? $"لا يوجد نشاط مسجل للوكيل {item.AgentName}"
                                : $"الوكيل {item.AgentName} غير نشط"
                            : item.IsActive
                                ? $"{item.AgentName} has no recorded activity"
                                : $"{item.AgentName} is inactive",
                        Message = arabic
                            ? item.IsActive
                                ? "لم يتم تسجيل أي نشاط طلاب لهذا الوكيل."
                                : "سجل الوكيل غير نشط حاليًا."
                            : item.IsActive
                                ? "No student activity has been recorded for this agent."
                                : "The agent record is currently inactive."
                    });
                }
            }

            return notifications
         .OrderBy(x => x.Severity == "Critical" ? 0 : 1)
         .ThenBy(x => x.AgentName)
         .Take(30)
         .ToList();
        }

        private static List<AgentStatisticsRowVM> ApplyFilters(
            List<AgentStatisticsRowVM> rows,
            string search,
            string health)
        {
            IEnumerable<AgentStatisticsRowVM> filtered = rows;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                filtered = filtered.Where(x =>
                    Contains(x.AgentName, term) ||
                    Contains(Convert.ToString(x.AgentCode), term) ||
                    Contains(x.AgentEmail, term) ||
                    Contains(x.AgentCity, term) ||
                    Contains(Convert.ToString(x.CountryId), term));
            }

            var normalizedHealth = NormalizeHealthFilter(health);
            if (normalizedHealth != "all")
                filtered = filtered.Where(x => x.HealthFilterValue == normalizedHealth);
            return filtered.ToList();
        }

        private static bool HasMissingDocuments(StudentRow item)
        {
            var hasCertificate =
                !string.IsNullOrWhiteSpace(item.StudentHighSchoolCertificate) ||
                !string.IsNullOrWhiteSpace(item.StudentHighSchoolCertificate2) ||
                !string.IsNullOrWhiteSpace(item.StudentHighSchoolCertificate3) ||
                !string.IsNullOrWhiteSpace(item.StudentHighSchoolCertificate4) ||
                !string.IsNullOrWhiteSpace(item.StudentHighSchoolCertificate5);
            var hasGrades =
                !string.IsNullOrWhiteSpace(item.StudentGradesReport) ||
                !string.IsNullOrWhiteSpace(item.StudentGradesReport1);
            return string.IsNullOrWhiteSpace(item.StudentProofOfIdentity) ||
                   !hasCertificate || !hasGrades;
        }

        private static bool IsPending(StudentRow x) =>
            x.StatusId == 2 || StatusEquals(x.StatusEnglish, "Pending");
        private static bool IsApproved(StudentRow x) =>
            x.StatusId == 3 || x.StatusId == 4 || x.StatusId == 6 ||
            StatusEquals(x.StatusEnglish, "Accepted") ||
            StatusEquals(x.StatusEnglish, "Student's Approved") ||
            StatusEquals(x.StatusEnglish, "Accepted with Condition");
        private static bool IsRejected(StudentRow x) =>
            x.StatusId == 5 || StatusEquals(x.StatusEnglish, "Rejected");
        private static bool IsUnderReview(StudentRow x) =>
            StatusEquals(x.StatusEnglish, "Under Review") ||
            StatusEquals(x.StatusEnglish, "Review");
        private static bool StatusEquals(string value, string expected) =>
            string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        private static bool Contains(string value, string term) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;

        private static string GetAgentName(AgentRow x, DashboardLanguage language)
        {
            if (language == DashboardLanguage.Arabic)
                return x.AgentNameArabic ?? x.AgentNameEnglish ?? "غير محدد";
            return x.AgentNameEnglish ?? x.AgentNameArabic ?? "N/A";
        }

        private static int HealthPriority(string health) =>
            health == "critical" ? 4 :
            health == "attention" ? 3 :
            health == "inactive" ? 2 : 1;

        private static string NormalizeSemester(string semester)
        {
            var value = (semester ?? "current").Trim().ToLowerInvariant();
            return value == "previous" || value == "all" ? value : "current";
        }

        private static string NormalizeHealthFilter(string health)
        {
            var value = (health ?? "all").Trim().ToLowerInvariant();
            if (value == "warning" || value == "needs attention") return "attention";
            return value == "healthy" || value == "attention" ||
                   value == "critical" || value == "inactive"
                ? value
                : "all";
        }

        private static string CsvValue(string value)
        {
            var safe = value ?? string.Empty;
            return "\"" + safe.Replace("\"", "\"\"") + "\"";
        }

        private static string ArabicHealth(string value)
        {
            if (value == "Healthy") return "جيد";
            if (value == "Needs Attention") return "يحتاج متابعة";
            if (value == "Critical") return "حرج";
            if (value == "Inactive") return "غير نشط";
            return value;
        }

        private sealed class StatisticsData
        {
            public string SemesterFilter { get; set; }
            public int? CurrentSemesterId { get; set; }
            public int? PreviousSemesterId { get; set; }
            public List<AgentRow> Agents { get; set; } = new List<AgentRow>();
            public List<StudentRow> Students { get; set; } = new List<StudentRow>();
        }

        private sealed class SemesterRow
        {
            public int? SemesterId { get; set; }
        }

        private sealed class AgentRow
        {
            public int AgentId { get; set; }
            public int? AgentCode { get; set; }
            public string AgentNameArabic { get; set; }
            public string AgentNameEnglish { get; set; }
            public string AgentEmail { get; set; }
            public string City { get; set; }
            public int? CountryId { get; set; }
            public int? Active { get; set; }
            public string AgentStatus { get; set; }
            public DateTime? ContractEndDate { get; set; }
        }

        private sealed class StudentRow
        {
            public int StudentId { get; set; }
            public int? AgentId { get; set; }
            public int? StatusId { get; set; }
            public int? SemesterId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string StatusEnglish { get; set; }
            public string StudentProofOfIdentity { get; set; }
            public string StudentHighSchoolCertificate { get; set; }
            public string StudentHighSchoolCertificate2 { get; set; }
            public string StudentHighSchoolCertificate3 { get; set; }
            public string StudentHighSchoolCertificate4 { get; set; }
            public string StudentHighSchoolCertificate5 { get; set; }
            public string StudentGradesReport { get; set; }
            public string StudentGradesReport1 { get; set; }
        }
    }
}
