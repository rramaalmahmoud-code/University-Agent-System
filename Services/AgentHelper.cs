using System.Data;
using Dapper;
namespace University_Agent_System.Services
{
    public class AgentHelper
    {
        private readonly IDbConnection _db;
        private readonly IHttpContextAccessor _httpContext;

        public AgentHelper(IDbConnection db, IHttpContextAccessor httpContext)
        {
            _db = db;
            _httpContext = httpContext;
        }

        public string GetAgentStatus()
        {
            var agentId = int.Parse(_httpContext.HttpContext.User.FindFirst("agentId").Value);
            var status = _db.QueryFirstOrDefault<string>("SELECT agentStatus FROM Agents WHERE agentId = @Id", new { Id = agentId });
            return status;
        }
    }

}
