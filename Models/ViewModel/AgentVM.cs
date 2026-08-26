namespace University_Agent_System.Models.ViewModel
{
    public class AgentVM
    {
        public int agentId { get; set; } //Primary Key
        public string? agentPhone { get; set; }
        public string? agentNameEnglish { get; set; }
        public string? agentNameArabic { get; set; }
        public string? city { get; set; }
        public string? agentEmail { get; set; }
        public string? agentStatus { get; set; } //

        public int? agentCode { get; set; }
        public string? country { get; set; }
    }
}
