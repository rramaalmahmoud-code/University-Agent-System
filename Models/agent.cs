namespace University_Agent_System.Models
{
    public class agent
    {
        public int agentId { get; set; } //Primary Key
        public int? agentCode { get; set; }
        public string? nationalId { get; set; }

        public string? agentNameArabic { get; set; }
        public string? agentNameEnglish { get; set; }
        public int? nationalityId { get; set; }
        public int? countryId { get; set; }
        public string? city { get; set; }
        public string? agentEmail { get; set; }
        public string? agentIban { get; set; }
        public string? passowrd { get; set; }
        public string? agentPhone { get; set; }
        public string? password { get; set; }
        public string? notes { get; set; }
        public string? commission { get; set; }
        public DateTime? contractStartDate { get; set; }
        public DateTime? contractEndDate { get; set; }
        public int? userId { get; set; }
        public int? active { get; set; }
        public string? agentStatus { get; set; } //
        public string? agentContract { get; set; }
        // Navigation Properties
        public user User { get; set; }
        public nationality Nationality { get; set; }
        public country Country { get; set; }

        public ICollection<student> Students { get; set; }
    }
}
