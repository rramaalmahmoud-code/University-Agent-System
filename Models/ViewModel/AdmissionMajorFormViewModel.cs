using System.ComponentModel.DataAnnotations;

namespace University_Agent_System.Models.ViewModel
{
    public class AdmissionMajorFormViewModel
    {
        public int AdmissionMajorId { get; set; }

        public int? OracleMajorNo { get; set; }

        [Required(ErrorMessage = "الاسم العربي للتخصص مطلوب")]
        [StringLength(
            500,
            ErrorMessage = "الاسم العربي يجب ألا يتجاوز 500 حرف"
        )]
        [Display(Name = "اسم التخصص بالعربية")]
        public string MajorNameAr { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الإنجليزي للتخصص مطلوب")]
        [StringLength(
            500,
            ErrorMessage = "الاسم الإنجليزي يجب ألا يتجاوز 500 حرف"
        )]
        [Display(Name = "اسم التخصص بالإنجليزية")]
        public string MajorNameEn { get; set; } = string.Empty;

        [Required(ErrorMessage = "الدرجة العلمية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "اختر الدرجة العلمية")]
        [Display(Name = "الدرجة العلمية")]
        public int? DegreeCode { get; set; }

        [Required(ErrorMessage = "الكلية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "اختر الكلية")]
        [Display(Name = "الكلية")]
        public int? FacultyNo { get; set; }

        [Display(Name = "متاح للقبول")]
        public bool IsEnabledForAdmission { get; set; } = true;

        public bool IsLocalOnly { get; set; }

        // للعرض فقط أثناء التعديل.
        public string? SourceMajorNameAr { get; set; }

        public string? SourceMajorNameEn { get; set; }
        [Required(ErrorMessage = "الفصل الدراسي مطلوب")]
        [Display(Name = "الفصل الدراسي")]
        public int SemesterId { get; set; }

        [Required(ErrorMessage = "نسبة الخصم مطلوبة")]
        [Range(
            typeof(decimal),
            "0",
            "100",
            ErrorMessage = "الخصم يجب أن يكون بين 0 و100"
        )]
        [Display(Name = "نسبة الخصم")]
        public decimal DiscountPercentage { get; set; }
    }
}