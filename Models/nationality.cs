namespace University_Agent_System.Models
{
    public class nationality
    {
        public int nationalityId { get; set; } //Primary Key
        public string? nationalityArabic { get; set; }
        public string? nationalityEnglish { get; set; }
        public int? active { get; set; }

        // Navigation Property
        public ICollection<student> Students { get; set; }
        public ICollection<agent> Agents { get; set; }
    }
}
