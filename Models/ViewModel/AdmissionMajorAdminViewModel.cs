namespace University_Agent_System.Models.ViewModel
{
    public class AdmissionMajorAdminViewModel
    {
        public int AdmissionMajorId { get; set; }

        public int? OracleMajorNo { get; set; }

        public string? MajorNameAr { get; set; }

        public string? MajorNameEn { get; set; }

        public string? SourceMajorNameAr { get; set; }

        public string? SourceMajorNameEn { get; set; }

        public int? DegreeCode { get; set; }

        public int? FacultyNo { get; set; }

        public bool IsLocalOnly { get; set; }

        public bool IsEnabledForAdmission { get; set; }

        public bool ExistsInOracle { get; set; }

        public DateTime? LastOracleSyncAt { get; set; }

        public int SemesterId { get; set; }

        public int? AdmissionMajorDiscountId { get; set; }

        public decimal DiscountPercentage { get; set; }
        public string? FacultyNameAr { get; set; }

        public string? FacultyNameEn { get; set; }

        public string? DepartmentNameAr { get; set; }

        public string? DepartmentNameEn { get; set; }
    }
}