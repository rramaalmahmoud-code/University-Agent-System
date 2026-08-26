namespace University_Agent_System.Models
{
    public class userType
    {
        public int userTypeId { get; set; }//Primary Key
        public string? userTypeArabic { get; set; }
        public string? userTypeEnglish { get; set; }
        public int? active { get; set; }


        // Navigation Property
        public ICollection<user> Users { get; set; }
    }
}
