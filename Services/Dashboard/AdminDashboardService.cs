using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using University_Agent_System.Models;
using University_Agent_System.Models.ViewModel;

namespace University_Agent_System.Services.Dashboard
{
    public sealed class AdminDashboardService : IAdminDashboardService
    {
        private const string MajorCacheKey = "AdminDashboard.Majors.v1";

        private readonly IDbConnection _db;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private static readonly SemaphoreSlim MajorCacheLock =
            new SemaphoreSlim(1, 1);

        public AdminDashboardService(
            IDbConnection db,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _db = db;
            _configuration = configuration;
            _cache = cache;
        }

        private static string GetDashboardAgentStatus(agent item)
        {
            var status = (item.agentStatus ?? string.Empty)
                .Replace("\u00A0", " ")
                .Trim()
                .ToLowerInvariant();

            if (status == "blocked" || status == "block")
                return "Blocked";

            if (status == "freezed" ||
                status == "frozen" ||
                status == "freeze" ||
                status == "frezed")
                return "Freezed";

            if (item.contractEndDate.HasValue &&
                item.contractEndDate.Value.Date < DateTime.Today)
                return "Expired";

            if (item.active != 1)
                return "Inactive";

            return "Active";
        }

        private static string GetDashboardAgentStatusDisplay(
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

        private static string GetDashboardAgentStatusCssClass(string status)
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

        public async Task<DashboardHomeViewModel> BuildHomeAsync(
            DashboardHomeRequest request,
            DashboardLanguage language,
            CancellationToken cancellationToken = default)
        {
            request ??= new DashboardHomeRequest();
            var page = Math.Max(request.Page, 1);
            var pageSize = Math.Min(Math.Max(request.PageSize, 5), 100);

            var data = await LoadDashboardDataAsync(
                request.Search,
                request.Semester,
                language,
                cancellationToken);

            var pendingStudents = data.Students
                .Where(x => IsPending(x.statusEnglish))
                .OrderByDescending(x => x.studentId)
                .ToList();

            var totalPages = Math.Max(
                1,
                (int)Math.Ceiling(pendingStudents.Count / (double)pageSize));
            page = Math.Min(page, totalPages);

            return new DashboardHomeViewModel
            {
                SearchTerm = request.Search,
                IntakeFilter = data.SemesterFilter,
                CurrentIntakeId = data.CurrentSemesterId,
                PreviousIntakeId = data.PreviousSemesterId,
                TotalStudents = data.Students.Count,
                TotalAgents = data.Agents.Count,
                PendingCount = pendingStudents.Count,
                ApprovedCount = data.Students.Count(x => IsApproved(x.statusEnglish)),
                RejectedCount = data.Students.Count(x => IsRejected(x.statusEnglish)),
                UnderReviewCount = data.Students.Count(x => IsUnderReview(x.statusEnglish)),
                StudentConfirmedCount = data.Students.Count(x => x.approvedByStudent),
                Agents = data.Agents,
                LatestStudents = data.Students
                    .OrderByDescending(x => x.studentId)
                    .Take(5)
                    .ToList(),
                AgentStatistics = data.AgentStatistics,
                Notifications = BuildNotifications(data.AgentStatistics, language),
                PendingStudents = pendingStudents
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                PendingTotalCount = pendingStudents.Count,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ApplicationStatusReportVM> BuildApplicationStatusReportAsync(
            string semester,
            DashboardLanguage language,
            CancellationToken cancellationToken = default)
        {
            var data = await LoadDashboardDataAsync(
                null,
                semester,
                language,
                cancellationToken);
            var total = data.Students.Count;

            var statusRows = data.Students
                .GroupBy(x => GetStatusDisplayName(x, language))
                .Select(group => new StatusSummaryVM
                {
                    StatusName = group.Key,
                    Count = group.Count(),
                    Percentage = total == 0
                        ? 0m
                        : Math.Round(group.Count() * 100m / total, 1)
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.StatusName)
                .ToList();

            return new ApplicationStatusReportVM
            {
                IntakeFilter = data.SemesterFilter,
                CurrentIntakeId = data.CurrentSemesterId,
                PreviousIntakeId = data.PreviousSemesterId,
                TotalStudents = total,
                PendingCount = data.Students.Count(x => IsPending(x.statusEnglish)),
                UnderReviewCount = data.Students.Count(x => IsUnderReview(x.statusEnglish)),
                ApprovedCount = data.Students.Count(x => IsApproved(x.statusEnglish)),
                RejectedCount = data.Students.Count(x => IsRejected(x.statusEnglish)),
                Statuses = statusRows
            };
        }

        public async Task<AgentPerformanceReportVM> BuildAgentPerformanceReportAsync(
            string search,
            string semester,
            string health,
            DashboardLanguage language,
            CancellationToken cancellationToken = default)
        {
            var data = await LoadDashboardDataAsync(
                null,
                semester,
                language,
                cancellationToken);
            IEnumerable<AgentStatisticsVM> rows = data.AgentStatistics;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                rows = rows.Where(x =>
                    ContainsIgnoreCase(x.AgentName, term) ||
                    Convert.ToString(x.AgentCode).Contains(term));
            }

            var normalizedHealth = NormalizeHealth(health);
            if (normalizedHealth != "all")
            {
                rows = rows.Where(x => string.Equals(
                    NormalizeHealth(x.HealthStatus),
                    normalizedHealth,
                    StringComparison.OrdinalIgnoreCase));
            }

            return new AgentPerformanceReportVM
            {
                SearchTerm = search,
                IntakeFilter = data.SemesterFilter,
                HealthFilter = normalizedHealth,
                CurrentIntakeId = data.CurrentSemesterId,
                PreviousIntakeId = data.PreviousSemesterId,
                Agents = rows
                    .OrderByDescending(x => x.ApprovalRate)
                    .ThenByDescending(x => x.TotalStudents)
                    .ThenBy(x => x.AgentName)
                    .ToList()
            };
        }

        private async Task<DashboardData> LoadDashboardDataAsync(
            string search,
            string semester,
            DashboardLanguage language,
            CancellationToken cancellationToken)
        {
            var semesterFilter = NormalizeSemester(semester);

            var semesterRows = (await _db.QueryAsync<SemesterRow>(
                new CommandDefinition(@"
SELECT TOP (2) semesterId AS SemesterId
FROM Students
WHERE active = 1
  AND semesterId IS NOT NULL
GROUP BY semesterId
ORDER BY semesterId DESC",
                    cancellationToken: cancellationToken))).ToList();

            int? currentSemesterId = semesterRows.Count > 0
                ? semesterRows[0].SemesterId
                : (int?)null;
            int? previousSemesterId = semesterRows.Count > 1
                ? semesterRows[1].SemesterId
                : (int?)null;
            int? selectedSemesterId = semesterFilter == "all"
                ? null
                : semesterFilter == "previous"
                    ? previousSemesterId
                    : currentSemesterId;

            // One minimal Students query. No SELECT *, no second Students read,
            // and no separate Agents/Statuses lookup for every student.
            var studentRows = (await _db.QueryAsync<StudentRow>(
                new CommandDefinition(@"
SELECT
    s.studentId AS StudentId,
    s.studentNameEnglish AS StudentNameEnglish,
    s.studentNameArabic AS StudentNameArabic,
    s.studentPhone AS StudentPhone,
    s.studentCode AS StudentCode,
    s.major_no AS MajorNo,
    s.agentId AS AgentId,
    s.studentApproval AS StudentApproval,
    COALESCE(a.agentNameEnglish, 'N/A') AS AgentNameEnglish,
    COALESCE(a.agentNameArabic, a.agentNameEnglish, N'غير محدد') AS AgentNameArabic,
    COALESCE(st.statusEnglish, 'N/A') AS StatusEnglish,
    COALESCE(st.statusArabic, N'غير محدد') AS StatusArabic
FROM Students s
LEFT JOIN Agents a ON a.agentId = s.agentId
LEFT JOIN Statuses st ON st.statusId = s.statusId AND st.active = 1
WHERE s.active = 1
  AND (@SemesterId IS NULL OR s.semesterId = @SemesterId)",
                    new { SemesterId = selectedSemesterId },
                    cancellationToken: cancellationToken))).ToList();

            var agents = (await _db.QueryAsync<agent>(
                new CommandDefinition(@"
SELECT
    agentId,
    agentCode,
    agentNameEnglish,
    agentNameArabic,
    agentEmail,
    agentPhone,
    city,
    countryId,
    agentStatus,
    contractEndDate,
    active
FROM Agents
ORDER BY agentNameEnglish",
                    cancellationToken: cancellationToken))).ToList();

            var majors = await GetMajorsAsync(cancellationToken);
            var students = studentRows.Select(row => MapStudent(row, majors)).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                students = students.Where(x => MatchesSearch(x, term)).ToList();
            }

            return new DashboardData
            {
                SemesterFilter = semesterFilter,
                CurrentSemesterId = currentSemesterId,
                PreviousSemesterId = previousSemesterId,
                Students = students,
                Agents = agents,
                AgentStatistics = BuildAgentStatistics(agents, students, language)
            };
        }

        private async Task<IReadOnlyDictionary<int, MajorName>> GetMajorsAsync(
            CancellationToken cancellationToken)
        {
            if (_cache.TryGetValue(
                MajorCacheKey,
                out IReadOnlyDictionary<int, MajorName> cachedMajors))
                return cachedMajors;

            await MajorCacheLock.WaitAsync(cancellationToken);
            try
            {
                if (_cache.TryGetValue(
                    MajorCacheKey,
                    out cachedMajors))
                    return cachedMajors;

                var connectionString =
                    _configuration.GetConnectionString("OracleConnection");
                await using var oracle = new OracleConnection(connectionString);
                await oracle.OpenAsync(cancellationToken);

                var rows = (await oracle.QueryAsync<MajorRow>(
                    new CommandDefinition(@"
SELECT
    major_no AS MajorNo,
    Major_Name_S AS EnglishName,
    Major_Name AS ArabicName
FROM major_info1_vw",
                        cancellationToken: cancellationToken))).ToList();

                cachedMajors = rows
                    .Where(x => x.MajorNo.HasValue)
                    .GroupBy(x => x.MajorNo.Value)
                    .ToDictionary(
                        x => x.Key,
                        x => new MajorName
                        {
                            English = x.First().EnglishName ?? "N/A",
                            Arabic = x.First().ArabicName ??
                                     x.First().EnglishName ??
                                     "غير محدد"
                        });

                _cache.Set(
                    MajorCacheKey,
                    cachedMajors,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                        Size = 1
                    });

                return cachedMajors;
            }
            finally
            {
                MajorCacheLock.Release();
            }
        }

        private static StudentWithMajorVM MapStudent(
            StudentRow row,
            IReadOnlyDictionary<int, MajorName> majors)
        {
            MajorName major = null;
            if (row.MajorNo.HasValue)
                majors.TryGetValue(row.MajorNo.Value, out major);

            return new StudentWithMajorVM
            {
                studentId = row.StudentId,
                studentNameEnglish = row.StudentNameEnglish,
                studentNameArabic = row.StudentNameArabic,
                studentPhone = row.StudentPhone,
                studentCode = row.StudentCode,
                Major_Name_S = major?.English ?? "N/A",
                Major_Name = major?.Arabic ?? "غير محدد",
                agentNameEnglish = row.AgentNameEnglish,
                agentNameArabic = row.AgentNameArabic,
                statusEnglish = row.StatusEnglish,
                statusArabic = row.StatusArabic,
                agentId = row.AgentId,
                approvedByStudent = row.StudentApproval == 1
            };
        }

        private static List<AgentStatisticsVM> BuildAgentStatistics(
            List<agent> agents,
            List<StudentWithMajorVM> students,
            DashboardLanguage language)
        {
            var studentsByAgent = students
                .Where(x => x.agentId.HasValue)
                .GroupBy(x => x.agentId.Value)
                .ToDictionary(x => x.Key, x => x.ToList());
            var result = new List<AgentStatisticsVM>(agents.Count);

            foreach (var item in agents)
            {
                var accountStatus = GetDashboardAgentStatus(item);
                var isContractExpired =
                    item.contractEndDate.HasValue &&
                    item.contractEndDate.Value.Date < DateTime.Today;

                if (!studentsByAgent.TryGetValue(item.agentId, out var rows))
                    rows = new List<StudentWithMajorVM>();

                var approved = rows.Count(x => IsApproved(x.statusEnglish));
                var rejected = rows.Count(x => IsRejected(x.statusEnglish));
                var pending = rows.Count(x => IsPending(x.statusEnglish));
                var decided = approved + rejected;
                var rejectionRate = decided == 0 ? 0m : rejected * 100m / decided;

                var healthStatus = "Healthy";
                var healthCssClass = "health-good";
                if (accountStatus == "Blocked" ||
                    accountStatus == "Freezed" ||
                    accountStatus == "Expired" ||
                    accountStatus == "Inactive")
                {
                    healthStatus = "Inactive";
                    healthCssClass = "health-inactive";
                }
                else if (pending >= 25 || rejectionRate >= 40m)
                {
                    healthStatus = "Critical";
                    healthCssClass = "health-critical";
                }
                else if (pending >= 15 || rejectionRate >= 25m)
                {
                    healthStatus = "Needs Attention";
                    healthCssClass = "health-warning";
                }

                result.Add(new AgentStatisticsVM
                {
                    AgentId = item.agentId,
                    AgentCode = item.agentCode,
                    AgentName = language == DashboardLanguage.Arabic
                        ? item.agentNameArabic ?? item.agentNameEnglish
                        : item.agentNameEnglish ?? item.agentNameArabic,
                    TotalStudents = rows.Count,
                    PendingCount = pending,
                    UnderReviewCount = rows.Count(x => IsUnderReview(x.statusEnglish)),
                    ApprovedCount = approved,
                    RejectedCount = rejected,
                    StudentConfirmedCount = rows.Count(x => x.approvedByStudent),
                    ApprovalRate = decided == 0
                        ? 0m
                        : Math.Round(approved * 100m / decided, 1),
                    HealthStatus = healthStatus,
                    HealthCssClass = healthCssClass,
                    AccountStatus = accountStatus,
                    AccountStatusDisplay = GetDashboardAgentStatusDisplay(
                        accountStatus,
                        language),
                    AccountStatusCssClass = GetDashboardAgentStatusCssClass(
                        accountStatus),
                    ContractEndDate = item.contractEndDate,
                    IsContractExpired = isContractExpired
                });
            }

            return result
                .OrderByDescending(x => x.PendingCount)
                .ThenBy(x => x.AgentName)
                .ToList();
        }

        private static List<DashboardNotificationVM> BuildNotifications(
            List<AgentStatisticsVM> statistics,
            DashboardLanguage language)
        {
            var statusNotifications = new List<DashboardNotificationVM>();
            var activityNotifications = new List<DashboardNotificationVM>();

            foreach (var item in statistics)
            {
                if (item.AccountStatus == "Blocked")
                {
                    statusNotifications.Add(new DashboardNotificationVM
                    {
                        Type = "Critical",
                        CssClass = "notice-critical",
                        Icon = "fas fa-ban",
                        Title = language == DashboardLanguage.Arabic
                            ? "وكيل محظور"
                            : "Blocked agent",
                        Message = language == DashboardLanguage.Arabic
                            ? $"الوكيل {item.AgentName} محظور ويحتاج إلى مراجعة الإدارة."
                            : $"{item.AgentName} is blocked and requires an administrator review.",
                        AgentId = item.AgentId
                    });
                }
                if (item.AccountStatus == "Freezed")
                {
                    statusNotifications.Add(new DashboardNotificationVM
                    {
                        Type = "Warning",
                        CssClass = "notice-warning",
                        Icon = "fas fa-snowflake",
                        Title = language == DashboardLanguage.Arabic
                            ? "وكيل مجمّد"
                            : "Frozen agent",
                        Message = language == DashboardLanguage.Arabic
                            ? $"حساب الوكيل {item.AgentName} مجمّد ويحتاج إلى متابعة."
                            : $"{item.AgentName}'s account is frozen and requires attention.",
                        AgentId = item.AgentId
                    });
                }
                if (item.IsContractExpired)
                {
                    var dateText = item.ContractEndDate?.ToString("yyyy-MM-dd") ?? "-";
                    statusNotifications.Add(new DashboardNotificationVM
                    {
                        Type = "Critical",
                        CssClass = "notice-critical",
                        Icon = "fas fa-file-contract",
                        Title = language == DashboardLanguage.Arabic
                            ? "عقد وكيل منتهي"
                            : "Expired agent contract",
                        Message = language == DashboardLanguage.Arabic
                            ? $"انتهى عقد الوكيل {item.AgentName} بتاريخ {dateText}."
                            : $"{item.AgentName}'s contract expired on {dateText}.",
                        AgentId = item.AgentId
                    });
                }

                if (item.PendingCount >= 15)
                {
                    var critical = item.PendingCount >= 25;
                    activityNotifications.Add(new DashboardNotificationVM
                    {
                        Type = critical ? "Critical" : "Warning",
                        CssClass = critical ? "notice-critical" : "notice-warning",
                        Icon = "fas fa-clock",
                        Title = language == DashboardLanguage.Arabic
                            ? "طلبات معلقة تحتاج إلى متابعة"
                            : "Pending applications require attention",
                        Message = language == DashboardLanguage.Arabic
                            ? $"لدى الوكيل {item.AgentName} عدد {item.PendingCount} من الطلبات المعلقة."
                            : $"{item.AgentName} has {item.PendingCount} pending applications.",
                        AgentId = item.AgentId
                    });
                }

                var decisions = item.ApprovedCount + item.RejectedCount;
                var rejectionRate = decisions == 0
                    ? 0m
                    : item.RejectedCount * 100m / decisions;
                if (decisions < 5 || rejectionRate < 25m)
                    continue;

                var criticalRejection = rejectionRate >= 40m;
                activityNotifications.Add(new DashboardNotificationVM
                {
                    Type = criticalRejection ? "Critical" : "Warning",
                    CssClass = criticalRejection
                        ? "notice-critical"
                        : "notice-warning",
                    Icon = "fas fa-exclamation-triangle",
                    Title = language == DashboardLanguage.Arabic
                        ? "ارتفاع نسبة الرفض"
                        : "High rejection rate",
                    Message = language == DashboardLanguage.Arabic
                        ? $"نسبة الرفض لدى الوكيل {item.AgentName} هي {rejectionRate:0.#}%."
                        : $"{item.AgentName}'s rejection rate is {rejectionRate:0.#}%.",
                    AgentId = item.AgentId
                });
            }

            return statusNotifications
                .OrderBy(x => x.Type == "Critical" ? 0 : 1)
                .Concat(activityNotifications
                    .OrderBy(x => x.Type == "Critical" ? 0 : 1))
                .Take(8)
                .ToList();
        }

        private static bool MatchesSearch(StudentWithMajorVM item, string term)
        {
            return ContainsIgnoreCase(item.studentNameEnglish, term) ||
                   ContainsIgnoreCase(item.studentNameArabic, term) ||
                   ContainsIgnoreCase(Convert.ToString(item.studentCode), term) ||
                   ContainsIgnoreCase(item.Major_Name_S, term) ||
                   ContainsIgnoreCase(item.Major_Name, term) ||
                   ContainsIgnoreCase(item.agentNameEnglish, term) ||
                   ContainsIgnoreCase(item.agentNameArabic, term) ||
                   ContainsIgnoreCase(item.statusEnglish, term) ||
                   ContainsIgnoreCase(item.statusArabic, term);
        }

        private static string GetStatusDisplayName(
            StudentWithMajorVM item,
            DashboardLanguage language)
        {
            var value = language == DashboardLanguage.Arabic
                ? item.statusArabic
                : item.statusEnglish;
            return string.IsNullOrWhiteSpace(value)
                ? language == DashboardLanguage.Arabic ? "غير محدد" : "N/A"
                : value.Trim();
        }

        private static string NormalizeSemester(string semester)
        {
            var value = (semester ?? "current").Trim().ToLowerInvariant();
            return value == "previous" || value == "all" ? value : "current";
        }

        private static string NormalizeHealth(string health)
        {
            var value = (health ?? "all").Trim().ToLowerInvariant();
            if (value == "needs attention") return "attention";
            return value == "critical" || value == "attention" || value == "healthy"
                ? value
                : "all";
        }

        private static bool ContainsIgnoreCase(string value, string term)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool StatusEquals(string value, string expected)
        {
            return string.Equals(
                value?.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPending(string value) => StatusEquals(value, "pending");
        private static bool IsRejected(string value) => StatusEquals(value, "rejected");
        private static bool IsUnderReview(string value) =>
            StatusEquals(value, "under review") || StatusEquals(value, "review");
        private static bool IsApproved(string value) =>
            StatusEquals(value, "approved") ||
            StatusEquals(value, "accepted") ||
            StatusEquals(value, "student's approved") ||
            StatusEquals(value, "accepted with condition");

        private sealed class DashboardData
        {
            public string SemesterFilter { get; set; }
            public int? CurrentSemesterId { get; set; }
            public int? PreviousSemesterId { get; set; }
            public List<agent> Agents { get; set; } = new List<agent>();
            public List<StudentWithMajorVM> Students { get; set; } =
                new List<StudentWithMajorVM>();
            public List<AgentStatisticsVM> AgentStatistics { get; set; } =
                new List<AgentStatisticsVM>();
        }

        private sealed class SemesterRow
        {
            public int? SemesterId { get; set; }
        }

        private sealed class StudentRow
        {
            public int StudentId { get; set; }
            public string StudentNameEnglish { get; set; }
            public string StudentNameArabic { get; set; }
            public string StudentPhone { get; set; }
            public string StudentCode { get; set; }
            public int? MajorNo { get; set; }
            public int? AgentId { get; set; }
            public int? StudentApproval { get; set; }
            public string AgentNameEnglish { get; set; }
            public string AgentNameArabic { get; set; }
            public string StatusEnglish { get; set; }
            public string StatusArabic { get; set; }
        }

        private sealed class MajorRow
        {
            public int? MajorNo { get; set; }
            public string EnglishName { get; set; }
            public string ArabicName { get; set; }
        }

        private sealed class MajorName
        {
            public string English { get; set; }
            public string Arabic { get; set; }
        }
    }
}
