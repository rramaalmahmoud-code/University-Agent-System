namespace University_Agent_System.Models
{
    public class status
    {
        public int statusId { get; set; }//Primary Key
        public string? statusArabic { get; set; }
        public string? statusEnglish { get; set; }
        public int? active { get; set; }

        // Navigation Property
        public ICollection<student> Students { get; set; }
    }
}
