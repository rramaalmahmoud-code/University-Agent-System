namespace University_Agent_System.Models
{
    public class user
    {
        public int userId { get; set; } //Primary Key
        public string? userNameArabic { get; set; }
        public string? userNameEnglish { get; set; }
        public string? userName { get; set; }
        public int? userTypeId { get; set; }
        public string? userEmail { get; set; }
        public string? userPhone { get; set; }
        public string? userPassword { get; set; }
        public int? active { get; set; }

        // Navigation Property
        public userType UserType { get; set; }
        public ICollection<agent> Agents { get; set; }
    }
}
