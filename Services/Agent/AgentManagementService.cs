using Dapper;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using University_Agent_System.Models.ViewModel;
using University_Agent_System.Services.Dashboard;

namespace University_Agent_System.Services.Agents
{
    public sealed class AgentManagementService : IAgentManagementService
    {
        private readonly IDbConnection _db;

        public AgentManagementService(IDbConnection db)
        {
            _db = db;
        }

        public async Task<AgentStatusUpdateResult> ToggleStatusAsync(
            int agentId,
            string requestedStatus,
            CancellationToken cancellationToken = default)
        {
            var status = NormalizeRequestedStatus(requestedStatus);
            if (agentId <= 0 || status == null)
                return status == null
                    ? AgentStatusUpdateResult.InvalidStatus
                    : AgentStatusUpdateResult.AgentNotFound;

            if (_db.State != ConnectionState.Open)
                _db.Open();

            using var transaction = _db.BeginTransaction();
            try
            {
                var state = await _db.QueryFirstOrDefaultAsync<AgentStatusRow>(
                    new CommandDefinition(@"
SELECT
    agentStatus AS AgentStatus,
    userId AS UserId
FROM Agents WITH (UPDLOCK, ROWLOCK)
WHERE agentId = @AgentId",
                        new { AgentId = agentId },
                        transaction,
                        cancellationToken: cancellationToken));

                if (state == null)
                {
                    transaction.Rollback();
                    return AgentStatusUpdateResult.AgentNotFound;
                }

                var newStatus = IsSameRestrictedStatus(status, state.AgentStatus)
                    ? "Active"
                    : status;

                await _db.ExecuteAsync(new CommandDefinition(@"
UPDATE Agents
SET agentStatus = @NewStatus
WHERE agentId = @AgentId",
                    new { NewStatus = newStatus, AgentId = agentId },
                    transaction,
                    cancellationToken: cancellationToken));

                if (state.UserId.HasValue)
                {
                    var userActive = newStatus == "Freezed" || newStatus == "Blocked"
                        ? 0
                        : 1;
                    await _db.ExecuteAsync(new CommandDefinition(@"
UPDATE Users
SET active = @Active
WHERE userId = @UserId",
                        new
                        {
                            Active = userActive,
                            UserId = state.UserId.Value
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                }

                transaction.Commit();
                return AgentStatusUpdateResult.Updated;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<AgentViewModel> GetAgentInfoAsync(
            int agentId,
            DashboardLanguage language,
            CancellationToken cancellationToken = default)
        {
            if (agentId <= 0)
                return null;

            var row = await _db.QueryFirstOrDefaultAsync<AgentInfoRow>(
                new CommandDefinition(@"
SELECT
    a.agentId AS AgentId,
    a.agentCode AS AgentCode,
    a.agentNameEnglish AS AgentNameEnglish,
    a.agentNameArabic AS AgentNameArabic,
    a.active AS Active,
    nat.nationalityEnglish AS NationalityEnglish,
    nat.nationalityArabic AS NationalityArabic,
    c.countryEnglish AS CountryEnglish,
    c.countryArabic AS CountryArabic,
    a.city AS City,
    a.agentEmail AS AgentEmail,
    a.agentPhone AS AgentPhone,
    a.notes AS Notes,
    a.agentStatus AS AgentStatus
FROM Agents a
LEFT JOIN Nationalities nat ON nat.nationalityId = a.nationalityId
LEFT JOIN Countries c ON c.countryId = a.countryId
WHERE a.agentId = @AgentId",
                    new { AgentId = agentId },
                    cancellationToken: cancellationToken));

            if (row == null)
                return null;

            var model = new AgentViewModel
            {
                agentId = row.AgentId,
                agentCode = row.AgentCode ?? 0,
                agentNameEnglish = row.AgentNameEnglish,
                agentNameArabic = row.AgentNameArabic,
                active = row.Active ?? 0,
                Nationality = language == DashboardLanguage.Arabic
                    ? row.NationalityArabic
                    : row.NationalityEnglish,
                Country = language == DashboardLanguage.Arabic
                    ? row.CountryArabic
                    : row.CountryEnglish,
                city = row.City,
                agentEmail = row.AgentEmail,
                agentPhone = row.AgentPhone,
                notes = row.Notes,
                agentStatus = row.AgentStatus
            };

            model.isActive = row.AgentStatus switch
            {
                "Freezed" => "Freezed",
                "Blocked" => "Blocked",
                _ => "Active"
            };
            return model;
        }

        private static string NormalizeRequestedStatus(string value)
        {
            var status = (value ?? string.Empty)
                .Replace("\u00A0", " ")
                .Trim()
                .ToLowerInvariant();

            if (status == "freezed" ||
                status == "frozen" ||
                status == "freeze" ||
                status == "frezed")
            {
                return "Freezed";
            }

            if (status == "blocked" ||
                status == "block")
            {
                return "Blocked";
            }

            if (status == "active")
            {
                return "Active";
            }

            return null;
        }

        private static bool IsSameRestrictedStatus(string requested, string current)
        {
            return (requested == "Freezed" || requested == "Blocked") &&
                   string.Equals(requested, current, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class AgentStatusRow
        {
            public string AgentStatus { get; set; }
            public int? UserId { get; set; }
        }

        private sealed class AgentInfoRow
        {
            public int AgentId { get; set; }
            public int? AgentCode { get; set; }
            public string AgentNameEnglish { get; set; }
            public string AgentNameArabic { get; set; }
            public int? Active { get; set; }
            public string NationalityEnglish { get; set; }
            public string NationalityArabic { get; set; }
            public string CountryEnglish { get; set; }
            public string CountryArabic { get; set; }
            public string City { get; set; }
            public string AgentEmail { get; set; }
            public string AgentPhone { get; set; }
            public string Notes { get; set; }
            public string AgentStatus { get; set; }
        }
    }
}
