namespace University_Agent_System.Models.ViewModel
{
    public class StudentFileViewModel
    {
        public string Title { get; set; }          // e.g. "National ID", "Picture"
        public string FileName { get; set; }       // actual file name
        public string FileUrl { get; set; }        // full path/url for download/view
        public DateTime? UploadedDate { get; set; } // fake or actual date
    }

}
