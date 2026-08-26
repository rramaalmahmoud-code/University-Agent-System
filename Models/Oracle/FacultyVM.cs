namespace University_Agent_System.Models.Oracle
{
    public class FacultyVM
    {
        public int Faculty_no { get; set; }
        public string? Faculty_Name { get; set; }
        public string? Faculty_Name_S { get; set; }
    }

    public class ProgramVM
    {
        public int major_no { get; set; }

        public string MAJOR_NAME { get; set; }
        public string Major_Name_S { get; set; }
        public int degree_code { get; set; }
        public int faculty_no { get; set; }
    }

    public class FacultyWithProgramsViewModel
    {
        public int Faculty_no { get; set; }
        public string Faculty_Name { get; set; }
        public string Faculty_Name_S { get; set; }
        public List<ProgramVM> Programs { get; set; } = new List<ProgramVM>();
    }

}
