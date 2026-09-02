namespace University_Agent_System.Services.Dashboard
{
    public sealed class DashboardHomeRequest
    {
        public string Search { get; set; }
        public string Semester { get; set; } = "current";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
