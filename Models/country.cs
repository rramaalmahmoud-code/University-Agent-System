namespace University_Agent_System.Models
{
    public class country
    {
        public int countryId { get; set; } //Primary Key
        public string? countryArabic { get; set; }
        public string? countryEnglish { get; set; }
        public int? active { get; set; }

        // Navigation Property
        public ICollection<student> Students { get; set; }
        public ICollection<agent> Agents { get; set; }
    }
}
