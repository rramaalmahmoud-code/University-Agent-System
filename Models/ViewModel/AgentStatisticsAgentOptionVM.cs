namespace University_Agent_System.Models.ViewModel
{
    public class AgentStatisticsAgentOptionVM
    {
        public int AgentId { get; set; }
        public int? AgentCode { get; set; }
        public string AgentName { get; set; }
        public string AccountStatus { get; set; }
        public bool IsContractExpired { get; set; }
    }
}
