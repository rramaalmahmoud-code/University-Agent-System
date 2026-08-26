using Microsoft.AspNetCore.Mvc.Rendering;
using University_Agent_System.Models.Oracle;

namespace University_Agent_System.Models.ViewModel
{
    public class StudentListViewModel
    {
        // Change the type of AcadimicYears to List<SelectListItem>
        public List<SelectListItem> AcadimicYears { get; set; }
        public string SearchTerm { get; set; }
        public string agentName { get; set; }
        public int TotalStudents => Students?.Count ?? 0;
        public int? SelectedSemester;
        public List<StudentWithMajorVM> Students { get; set; } = new();
        public List<StudentWithMajorVM> PendingStudents { get; set; } = new();    // Pending students (always displayed)
        public string SelectedAcademicYear { get; set; } // ⭐️ Add this
        public int? agentId { get; set; }
        public List<StudentWithMajorVM> FirstSemesterStudents { get; set; }
        public List<StudentWithMajorVM> SecondSemesterStudents { get; set; }
        public List<StudentWithMajorVM> SummerSemesterStudents { get; set; }
        public List<SelectListItem> Semesters { get; set; }  // Add this
        public int? SelectedCountryId { get; set; }
        public int? SelectedNationalityId { get; set; }

        public List<SelectListItem> Countries { get; set; } = new();
        public List<SelectListItem> Nationalities { get; set; } = new();


        // Pagination
        public int PendingTotalCount { get; set; }
        public int StudentTotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public List<agent> Agents { get; set; } = new List<agent>();


        public List<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
        public int? SelectedStatusId { get; set; }

    }


}
