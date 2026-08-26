using System.ComponentModel.DataAnnotations;

namespace University_Agent_System.Models
{
    public class student
    {
        public int studentId { get; set; } // Primary Key
        public long studentNumber { get; set; }//Primary Key

        public string Password { get; set; }


        public string? nationalId { get; set; }
        public string? studentNameArabic { get; set; }
        public string? studentNameEnglish { get; set; }

        public int? nationalityId { get; set; }
        public int? countryId { get; set; }

        public string? city { get; set; }
        public string? cityAdd { get; set; }          // NEW

        public string? studentEmail { get; set; }
        public string? studentPhone { get; set; }
        public string? studentSchool { get; set; }
        public string? studentGPA { get; set; }

        public int? degreeId { get; set; }
        public int? major_no { get; set; }
        public int? Faculty_no { get; set; }
        public int? semesterId { get; set; }

        public int? agentId { get; set; }
        public int? statusId { get; set; }

        public string? studentCode { get; set; }

        // ===== Names (Arabic / English) =====
        public string? ArabicFirstName { get; set; }
        public string? ArabicFatherName { get; set; }
        public string? ArabicGrandFatherName { get; set; }
        public string? ArabicFamilyName { get; set; }

        public string? EnglishFirstName { get; set; }
        public string? EnglishFatherName { get; set; }
        public string? EnglishGrandFatherName { get; set; }
        public string? EnglishFamilyName { get; set; }

        public string? motherName { get; set; }

        // ===== Dates =====
        public DateTime? dateOfBirth { get; set; }
        public DateTime? createdDate { get; set; }
        public DateTime? expiredDate { get; set; }

        public DateTime? CreatedAt { get; set; }

        // ===== Identity / Documents =====
        public string? docId { get; set; }

        public string? studentPicture { get; set; }
        public string? studentProof_of_Identity { get; set; }
        public string? studentHigh_School_Certificate { get; set; }
        public string? studentHigh_School_Certificate2 { get; set; }
        public string? studentHigh_School_Certificate3 { get; set; }
        public string? studentHigh_School_Certificate4 { get; set; }
        public string? studentHigh_School_Certificate5 { get; set; }
        public string? studentGrades_Report { get; set; }
        public string? studentGrades_Report1 { get; set; }
        public string? studentBachelor_Certification { get; set; }

        // ===== Academic / Admission =====
        public int? isTransfer { get; set; }
        public int? isDiploma { get; set; }

        public int? isDisabled { get; set; }
        public string? disabilityType { get; set; }

        public int? isPreviousAAU { get; set; }
        public int? previousStudentId { get; set; }
        public string? previousMajor { get; set; }

        public string? schoolBranch { get; set; }
        public string? certificateType { get; set; }
        public string? certificateYear { get; set; }
        public int? seatNumber { get; set; }

        public int? countryId0 { get; set; }
        public int? countryId1 { get; set; }
        public int? countryId2 { get; set; }
        public int? countryId3 { get; set; }

        public string? studentFaculty { get; set; }
        public string? schoolMajor { get; set; }

        public string? certificateYearDip { get; set; }
        public string? studentDiplomaGPA { get; set; }

        public string? studentUniversity { get; set; }
        public string? admissionType { get; set; }

        // ===== Referrers =====
        public string? Referrer1Name { get; set; }
        public string? Referrer1Relation { get; set; }
        public string? Referrer1Phone { get; set; }

        public string? Referrer2Name { get; set; }
        public string? Referrer2Relation { get; set; }
        public string? Referrer2Phone { get; set; }

        // ===== Legal / Status =====
        public int? LegalDeclaration { get; set; }
        public int? studentGender { get; set; }

        public int? active { get; set; }
        public int? studentApproval { get; set; }

        public string? approvalCondition { get; set; }
        public string? rejectionReason { get; set; }

        // ===== Discounts =====
        public decimal discountPercentage { get; set; }
        public decimal requiredDiscount { get; set; }

        // ===== Navigation Properties =====
        public nationality Nationality { get; set; }
        public country Country { get; set; }
        public degree Degree { get; set; }
        public agent Agent { get; set; }
        public status Status { get; set; }
    }
}
