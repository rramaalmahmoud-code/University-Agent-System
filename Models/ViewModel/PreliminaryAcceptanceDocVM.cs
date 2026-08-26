namespace University_Agent_System.Models.ViewModel
{
    public class PreliminaryAcceptanceDocVM
    {
        public int StudentId { get; set; }

        public string StudentNameArabic { get; set; }
        public string StudentNameEnglish { get; set; }
        public int StudentGender { get; set; } // 1 ذكر, 2 أنثى (عدّل حسب نظامك)

        public string NationalityText { get; set; } // مثال: "اردنية" أو English fallback
        public string DegreeText { get; set; }      // مثال: "الماجستير"
        public string MajorName { get; set; }       // مثال: "إدارة الاعمال"
        public string FacultyName { get; set; }     // مثال: "الدراسات العليا"

        public int SemesterId { get; set; }         // مثال 20251
        public string SemesterText { get; set; }    // مثال: "الفصل الأول" أو "الفصل الدراسي"
        public string AcademicYearText { get; set; } // مثال: "2025/2026"

        public int StatusId { get; set; }           // 3 أو 6
        public string ApprovalCondition { get; set; }

        public List<string> RequiredDocuments { get; set; } = new List<string>();

        public DateTime LetterDate { get; set; } = DateTime.Today;

    }
}
