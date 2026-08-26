namespace University_Agent_System.Models.ViewModel
{
    public class StudentMajorOptionViewModel
    {
        public int AdmissionMajorId { get; set; }

        public int? OracleMajorNo { get; set; }

        public string MajorNameAr { get; set; } =
            string.Empty;

        public string MajorNameEn { get; set; } =
            string.Empty;

        public int FacultyNo { get; set; }

        public int DegreeCode { get; set; }

        public decimal DiscountPercentage { get; set; }

        public bool IsEnabledForAdmission { get; set; }
    }
}