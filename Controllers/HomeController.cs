using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using University_Agent_System.Data;
using University_Agent_System.Models;
using Dapper;
using University_Agent_System.Models.Oracle;
using University_Agent_System.Models.ViewModel;
using Oracle.ManagedDataAccess.Client;
using Microsoft.AspNetCore.Authorization;
using University_Agent_System.Services;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class HomeController : Controller
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly IDbConnection _oracleDb; // اتصال خاص بالأوراكل
    private readonly AcademicService _academicService;
    private readonly StudentsBySemester _studentsBySemester;
    public HomeController(IDbConnection db, IConfiguration configuration, IWebHostEnvironment env, IDbConnection oracleDb, AcademicService academicService, StudentsBySemester studentsBySemester)
    {
        _db = db;
        _configuration = configuration;
        _env = env;
        string oracleConnStr = configuration.GetConnectionString("OracleConnection");
        _oracleDb = new OracleConnection(oracleConnStr);
        _academicService = academicService;
        _studentsBySemester = studentsBySemester;
    }
    //[Authorize]
    //public IActionResult Home()
    //{
    //    string studentSql = "SELECT * FROM Students";
    //    var students = _db.Query<student>(studentSql).ToList();

    //    string majorSql = "SELECT major_no, Major_Name_S FROM major_info1_vw";
    //    var majors = _oracleDb.Query<ProgramVM>(majorSql).ToList();

    //    var studentWithMajor = students.Select(s => new StudentWithMajorVM
    //    {
    //        studentNameEnglish = s.studentNameEnglish,
    //        studentPhone = s.studentPhone,
    //        Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A"
    //    }).ToList();

    //    return View(studentWithMajor);
    //}
    private string GetCurrentAcademicYear()
    {
        int year = DateTime.Now.Year;
        int month = DateTime.Now.Month;

        // Adjust based on your academic calendar logic
        if (month >= 9)
            return $"{year}-{year + 1}";
        else
            return $"{year - 1}-{year}";
    }
    [Authorize]
    public IActionResult Home(StudentListViewModel model, string selectedAcademicYear)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        // Block access for specific roles
        if (userRole == "Agent" || userRole == "Super Admin" || userRole == "Admin")
        {
            return Forbid(); // or RedirectToAction("AccessDenied")
        }

        // Fetch academic years
        model.AcadimicYears = _academicService.GetAcademicYears();
        // If no year is selected, default to the current academic year
        if (string.IsNullOrEmpty(selectedAcademicYear))
        {
            selectedAcademicYear = GetCurrentAcademicYear(); // Implement this method
        }

        model.SelectedAcademicYear = selectedAcademicYear;
        int? agentId = null;
        string? agentName = null;

        var jwt = Request.Cookies["jwt"];
        if (!string.IsNullOrEmpty(jwt))
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var userType = token.Claims.FirstOrDefault(c => c.Type == "userType")?.Value;

            if (userType == "Agent")
            {
                var agentIdClaim = token.Claims.FirstOrDefault(c => c.Type == "agentId");
                var agentNameClaim = token.Claims.FirstOrDefault(c => c.Type == "agentName");
                if (agentIdClaim != null && int.TryParse(agentIdClaim.Value, out int extractedAgentId))
                {
                    agentId = extractedAgentId;
                }
                if (!string.IsNullOrEmpty(agentNameClaim?.Value))
                {
                    agentName = agentNameClaim.Value;
                }

            }
        }

    
        // Fetch students for that academic year and all semesters
        model.FirstSemesterStudents = _studentsBySemester.GetStudentsBySemester(selectedAcademicYear, 1, agentId);
        model.SecondSemesterStudents = _studentsBySemester. GetStudentsBySemester(selectedAcademicYear, 2, agentId);
        model.SummerSemesterStudents = _studentsBySemester.GetStudentsBySemester(selectedAcademicYear, 3, agentId);


        return View(model);
    }

    //public IActionResult Index(string culture)
    //{
    //    // Manually set the content based on the culture
    //    if (culture == "ar")
    //    {
    //        ViewData["Message"] = "مرحبًا بكم في نظام وكيل الجامعة"; // Arabic message
    //    }
    //    else
    //    {
    //        ViewData["Message"] = "Welcome to the University Agent System"; // English message
    //    }

    //    return View();
    //}
    public IActionResult SetLanguage(string culture)
    {
        if (string.IsNullOrEmpty(culture))
        {
            culture = "en"; // Default to English if not provided
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        return LocalRedirect($"/{culture}");
    }




}
