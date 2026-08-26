using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

using University_Agent_System.Models.Oracle;

namespace University_Agent_System.Models.ViewModel
{
    public class StudentViewModel
    {
        public int studentId { get; set; }//Primary Key
        public long studentNumber { get; set; }//Primary Key
        public string Password { get; set; }

        [Required(ErrorMessage = "Student First Name is required")]
        public string ArabicFirstName { get; set; }
        [Required(ErrorMessage = "Student Father Name is required")]
        public string ArabicFatherName { get; set; }
        [Required(ErrorMessage = "Student GrandFather Name is required")]
        public string ArabicGrandFatherName { get; set; }
        [Required(ErrorMessage = "Student Family Name is required")]
        public string ArabicFamilyName { get; set; }
        [Required(ErrorMessage = "Student Name is required")]
  
        public string? studentNameArabic { get; set; }
        [Required(ErrorMessage = "Student First Name is required")]
        public string EnglishFirstName { get; set; }
        [Required(ErrorMessage = "Student Father Name is required")]
        public string EnglishFatherName { get; set; }
        [Required(ErrorMessage = "Student GrandFather Name is required")]
        public string EnglishGrandFatherName { get; set; }
        [Required(ErrorMessage = "Student Family Name is required")]
        public string EnglishFamilyName { get; set; }
        [Required(ErrorMessage = "Student Name is required")]

        public string? studentNameEnglish { get; set; }
        [Required(ErrorMessage = "ID Number is required")]
        public string? nationalId { get; set; }
        [Required(ErrorMessage = "Mother Name is required")]
        public string? motherName { get; set; }
        [Required(ErrorMessage = "dateOfBirth is required")]
        public DateOnly? dateOfBirth { get; set; }
        [Required(ErrorMessage = "createdDate is required")]
        public DateOnly? createdDate { get; set; }
        [Required(ErrorMessage = "expiredDate is required")]
        public DateOnly? expiredDate { get; set; }
        [Required(ErrorMessage = "Nationality is required")]
        public int? nationalityId { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public int? countryId0 { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public int? countryId { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public int? countryId1 { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public int? countryId2 { get; set; }
        [Required(ErrorMessage = "Country is required")]
        public int? countryId3 { get; set; }
        public int? seatNumber { get; set; }
        public int? isDisabled { get; set; } = 0;
        public int? isPreviousAAU { get; set; } = 0;
        public string? previousMajor { get; set; }
        public string? docId { get; set; }
        public string? disabilityType { get; set; }
        public string? schoolBranch { get; set; }
        public string? certificateType { get; set; }
        public string? certificateYear { get; set; }
        public string? certificateYearDip { get; set; }
        public string? admissionType { get; set; }
        [Required(ErrorMessage = "يجب الموافقة على التعهّد قبل إرسال الطلب")]
        public bool LegalDeclaration { get; set; }



        public int? previousStudentId { get; set; }
    
        [Required(ErrorMessage = "Student Phone is required")]
        public string? studentPhone { get; set; }
        [Required(ErrorMessage = "Student Email is required")]
        public string? studentEmail { get; set; }
       
        [Required(ErrorMessage = "Student GPA is required")]
        public string? studentGPA { get; set; }
        [Required(ErrorMessage = "Student GPA is required")]
        public string? studentDiplomaGPA { get; set; }
        [Required(ErrorMessage = "Acadmic Degree is required")]
        public int? degreeId { get; set; }
        //[Required(ErrorMessage = "Program is required")]
        //public int? major_no { get; set; }
        /*
 * المفتاح المحلي من جدول AdmissionMajors.
 * هذا هو الحقل الذي تختاره القائمة وترسله للسيرفر.
 */
        [Required(ErrorMessage = "Program is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a program")]
        public int? AdmissionMajorId { get; set; }

        /*
         * رقم التخصص الرسمي في Oracle.
         * لا نثق بالقيمة القادمة من المتصفح؛ السيرفر يملؤها
         * باستخدام AdmissionMajorId.
         *
         * يمكن أن يكون NULL عندما يكون التخصص محليًا ولم تتم
         * إضافته بعد في Oracle.
         */
        public int? major_no { get; set; }
        [Required(ErrorMessage = "Faculty is required")]
        public int? Faculty_no { get; set; }
        [Required(ErrorMessage = "School is required")]
        public string?    studentSchool { get; set; }
        [Required(ErrorMessage = "University is required")]
        public string? studentUniversity { get; set; }
        [Required(ErrorMessage = "Faculty is required")]
        public string? studentFaculty { get; set; }
        [Required(ErrorMessage = "Major is required")]
        public string? schoolMajor { get; set; }
        //public int? FacultyId { get; set; }
        public int? statusId { get; set; }
        [Required(ErrorMessage = "City is required")]
        public string? city { get; set; }
        public string? cityAdd { get; set; }

        public int? semesterId { get; set; }

        public int? isTransfer { get; set; }
        public int? isDiploma { get; set; }
        //[Required(ErrorMessage = "Agent is required")]
        public int? agentId { get; set; }
        [Required(ErrorMessage = "Student Gender is required")]
        public int? studentGender { get; set; }
     
        public string? studentCode { get; set; }
        public string? Referrer1Name { get; set; }
        public string? Referrer1Relation { get; set; }
        public string? Referrer1Phone { get; set; }
        public string? Referrer2Name { get; set; }
        public string? Referrer2Relation { get; set; }
        public string? Referrer2Phone { get; set; }
        public IFormFile? studentPicture { get; set; }
        public string? studentPicturePath { get; set; }
        //[Required(ErrorMessage = "studentProof_of_Identity is required")]
        public IFormFile? studentProof_of_Identity { get; set; }
        public string? studentProof_of_IdentityPath { get; set; }

        //[Required(ErrorMessage = "High School Certificate is required")]
        public IFormFile? studentHigh_School_Certificate { get; set; }
        public string? studentHigh_School_CertificatePath { get; set; }
        public IFormFile? studentHigh_School_Certificate2 { get; set; }
        public string? studentHigh_School_CertificatePath2 { get; set; }
        public IFormFile? studentHigh_School_Certificate3 { get; set; }
        public string? studentHigh_School_CertificatePath3 { get; set; }
        public IFormFile? studentHigh_School_Certificate4 { get; set; }
        public string? studentHigh_School_CertificatePath4 { get; set; }
        public IFormFile? studentHigh_School_Certificate5 { get; set; }
        public string? studentHigh_School_CertificatePath5 { get; set; }
        //[Required(ErrorMessage = "Academic Transcript is required(Transfer)")]
        public IFormFile? studentGrades_Report { get; set; }
        public string? studentGrades_ReportPath { get; set; }

        //[Required(ErrorMessage = "Academic Transcript is required (Diploma)")]
        public IFormFile? studentGrades_Report1 { get; set; }
        public string? studentGrades_ReportPath1 { get; set; }
        //[Required(ErrorMessage = "Bachelor's Degree Certificate is required")]
        public IFormFile? studentBachelor_Certification { get; set; }
        public string? studentBachelor_CertificationPath { get; set; }


        public int SelectedDegreeId { get; set; }  // 👈 Add this
        public decimal discountPercentage { get; set; }
        public decimal requiredDiscount { get; set; }


        // For displaying dropdowns
        public List<nationality>? Nationalities { get; set; }
        public List<country>? Countries { get; set; }
        public List<degree>? Degrees { get; set; }
        public List<agent>? Agents { get; set; }
        public List<FacultyWithProgramsViewModel>? Faculties { get; set; }
    }

}
