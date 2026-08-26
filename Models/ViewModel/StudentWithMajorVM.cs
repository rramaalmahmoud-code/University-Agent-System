namespace University_Agent_System.Models.ViewModel
{
    public class StudentWithMajorVM
    {
        public string studentNameEnglish { get; set; }
        public string studentNameArabic { get; set; }
        public string studentPhone { get; set; }
        public string Major_Name_S { get; set; }
        public string Major_Name { get; set; }
        public string country { get; set; }
        public string? studentCode { get; set; }
        public string? agentNameEnglish { get; set; }
        public string? agentNameArabic { get; set; }
        public string? statusEnglish { get; set; }
        public string? statusArabic { get; set; }
        public int studentId { get; set; }//Primary Key
        public bool approvedByStudent { get; set; }
        public int? semesterId { get; set; }
        public int? agentId { get; set; }
        public int major_no { get; set; }
        public string? approvalCondition { get; set; }
        public string? rejectionReason { get; set; }
        public decimal? requiredDiscount { get; set; }
        // ✅ add:
        public int statusId { get; set; }
        public bool CanShowPreAcceptanceDoc { get; set; }

    }

}
