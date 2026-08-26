namespace University_Agent_System.Models
{
    public class degree
    {
        public int degreeId { get; set; }//Primary Key
        public string? degreeArabic { get; set; }
        public string? degreeEnglish { get; set; }
        public int? active { get; set; }

        // Navigation Property
        public ICollection<student> Students { get; set; }
    }
}
