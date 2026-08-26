namespace University_Agent_System.Models.DTO
{
    public class ProgramDto
    {
        public int programId { get; set; }
        public string programNameEnglish { get; set; }
    }
    public class FacultyWithProgramsViewModel
    {
        public int facultyId { get; set; }
        public string facultyNameEnglish { get; set; }
        public List<ProgramDto> Programs { get; set; }
    }
}
