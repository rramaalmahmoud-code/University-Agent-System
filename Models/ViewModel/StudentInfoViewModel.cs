namespace University_Agent_System.Models.ViewModel
{
    public class StudentInfoViewModel
    {
        public string studentNameEnglish { get; set; }
        public string studentNameArabic { get; set; }
        public string studentId { get; set; }
        public string Nationality { get; set; }        
        public string Country { get; set; }
        public string studentCode { get; set; }

        public string city { get; set; }
        public string nationalId { get; set; }

        public string studentEmail { get; set; }
        public string studentPhone { get; set; }
        public string Degree { get; set; }         
        public string Major_Name_S { get; set; } 
        public string Major_Name { get; set; } 
        public string Faculty_Name_S { get; set; } 
        public string Faculty_Name { get; set; } 
        
        public string Semester { get; set; }      
        public string studentPicture { get; set; }
        public string studentProof_of_Identity { get; set; }
        public string studentHigh_School_Certificate { get; set; }
        public string studentHigh_School_Certificate2 { get; set; }
        public string studentHigh_School_Certificate3 { get; set; }
        public string studentHigh_School_Certificate4 { get; set; }
        public string studentHigh_School_Certificate5 { get; set; }
        public string studentGrades_Report { get; set; }
        public string studentGrades_Report1 { get; set; }
        public string studentBachelor_Certification { get; set; }
        public string isApproved { get; set; }
        public string Status { get; set; }
        public int active { get; set; }
        public string isActive { get; set; }
        public string? approvalCondition { get; set; }
        public DateTime? CreatedAt { get; set; } // <-- NEW COLUMN
        public int? studentApproval { get; set; }

        public List<StudentFileViewModel> Files { get; set; } = new();


    }

}
