using Dapper;
using Microsoft.AspNetCore.Mvc;
using University_Agent_System.Models.Oracle;
using University_Agent_System.Models.ViewModel;
using University_Agent_System.Models;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using University_Agent_System.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Diagnostics.Metrics;
using PhoneNumbers;
using System.Text.RegularExpressions;

namespace University_Agent_System.Controllers
{
 
    public class AdminEnController : Controller
    {
        private readonly IDbConnection _db;
        private readonly IDbConnection _oracleDb; // اتصال خاص بالأوراكل
        private readonly IConfiguration _configuration;
        private readonly AcademicService _academicService;
        private readonly StudentsBySemester _studentsBySemester;
        private readonly IWebHostEnvironment _env;
        private readonly IWhatsAppService _whatsAppService;

       
        public AdminEnController(IDbConnection db, IDbConnection oracleDb, IConfiguration configuration, AcademicService academicService, StudentsBySemester studentsBySemester, IWebHostEnvironment env, IWhatsAppService whatsAppService)
        {
            _db = db;
            string oracleConnStr = configuration.GetConnectionString("OracleConnection");
            _oracleDb = new OracleConnection(oracleConnStr);
            _configuration = configuration;
            _academicService = academicService;
            _studentsBySemester = studentsBySemester;
            _env = env;
            _whatsAppService = whatsAppService;

        }


        public IActionResult Index()
        {
            return View();
        }

        // Get all students with "Pending" status
        private List<StudentWithMajorVM> GetPendingStudents()
        {
            // Get all students
            string studentSql = "SELECT * FROM Students WHERE active = 1";
            var students = _db.Query<student>(studentSql).ToList();

            // Get all statuses
            string statusSql = "SELECT statusId, statusEnglish FROM Statuses WHERE active = 1";
            var statuses = _db.Query<status>(statusSql).ToList();

            // Get statusId for "Pending"
            var pendingStatusId = statuses.FirstOrDefault(st => st.statusEnglish.ToLower() == "pending")?.statusId;

            if (pendingStatusId == null)
            {
                return new List<StudentWithMajorVM>();
            }

            // Get all agents
            string agentSql = "SELECT agentId, agentNameEnglish FROM Agents WHERE active = 1";
            var agents = _db.Query<agent>(agentSql).ToList();

            // Get all majors
            string majorSql = "SELECT major_no, Major_Name_S FROM major_info1_vw";
            var majors = _oracleDb.Query<ProgramVM>(majorSql).ToList();

            // Filter students with "Pending" status only and map them
            return students
                .Where(s => s.statusId == pendingStatusId)
                .Select(s => new StudentWithMajorVM
                {
                    studentNameEnglish = s.studentNameEnglish,
                    studentPhone = s.studentPhone,
                    studentCode = s.studentCode,
                    Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A",
                    agentNameEnglish = agents.FirstOrDefault(a => a.agentId == s.agentId)?.agentNameEnglish ?? "N/A",
                    statusEnglish = statuses.FirstOrDefault(st => st.statusId == s.statusId)?.statusEnglish ?? "N/A",
                    studentId = s.studentId
                })
                .ToList();
        }


        private List<student> GetAllStudents()
        {
            string sql = "SELECT * FROM Students WHERE active = 1"; // Or your correct table
            return _db.Query<student>(sql).ToList();
        }

        private List<agent> GetAllAgents()
        {
            string sql = "SELECT * FROM Agents WHERE active = 1"; // Or your correct table
            return _db.Query<agent>(sql).ToList();
        }


        // Search students by agent name (case-insensitive)
        private (List<StudentWithMajorVM> Students, int? agentId, string agentName) SearchStudentsByAgent(List<student> students, string search)
        {
            // Get all agents
            string agentSql = "SELECT agentId, agentNameEnglish ,agentCode FROM Agents WHERE active = 1";
            var agents = _db.Query<agent>(agentSql).ToList();

            // Get majors from Oracle
            string majorSql = "SELECT major_no, Major_Name_S FROM major_info1_vw";
            var majors = _oracleDb.Query<ProgramVM>(majorSql).ToList();

            // Get all statuses
            string statusSql = "SELECT statusId, statusEnglish FROM Statuses WHERE active = 1";
            var statuses = _db.Query<status>(statusSql).ToList();

            if (string.IsNullOrEmpty(search))
            {
                return (new List<StudentWithMajorVM>(), null, null);
            }

    
            // Try parse search string to int (for agentCode)
            bool isNumeric = int.TryParse(search, out int agentCodeSearch);
            // Find agent by name (string match) or code (exact int match)
            var matchedAgent = agents.FirstOrDefault(a =>
        ((!string.IsNullOrEmpty(a.agentNameEnglish) &&
                a.agentNameEnglish.ToLower().Contains(search.ToLower())) ||
        (isNumeric && a.agentCode == agentCodeSearch)));
            int? matchedAgentId = matchedAgent?.agentId;
            string matchedAgentName = matchedAgent?.agentNameEnglish ?? "N/A";

            // Filter students who belong to that agent
            var filteredStudents = students
                .Where(s => s.agentId == matchedAgentId)
                .Select(s => new StudentWithMajorVM
                {
                    studentNameEnglish = s.studentNameEnglish,
                    studentPhone = s.studentPhone,
                    studentCode = s.studentCode,
                    Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A",
                    agentNameEnglish = matchedAgentName,
                    statusEnglish = statuses.FirstOrDefault(st => st.statusId == s.statusId)?.statusEnglish ?? "N/A",
                    studentId = s.studentId,
                    agentId = s.agentId,
                     approvedByStudent = s.studentApproval == 1

                }).ToList();

            return (filteredStudents, matchedAgentId, matchedAgentName);
        }
        [Authorize(Roles = "Admin,Super Admin")]

        // Action to return the home page with students based on agent search
        //public IActionResult Home(string search, int page = 1, int pageSize = 10)
        //{
        //    // Get pending students
        //    var pendingStudents = GetPendingStudents();

        //    // Get ALL students, not just pending
        //    var allStudents = GetAllStudents();
        //    // Apply pagination on pending students
        //    var totalPending = pendingStudents.Count;
        //    var pagedPending = pendingStudents
        //                        .Skip((page - 1) * pageSize)
        //                        .Take(pageSize)
        //                        .ToList();

        //    // Get both the filtered students and agentId
        //    var (studentsWithMajor, agentId, agentName) = SearchStudentsByAgent(allStudents, search);


        //    // Prepare the model to return
        //    var model = new StudentListViewModel
        //    {
        //        agentName = agentName,
        //        agentId = agentId ,
        //        SearchTerm = search,
        //        Students = studentsWithMajor,
        //        PendingStudents = pagedPending, // pending students
        //          PendingTotalCount = totalPending,
        //        CurrentPage = page,
        //        PageSize = pageSize

        //    };

        //    return View("~/Views/En/Admin/Home.cshtml", model);
        //}
        // Action to return the home page with students based on agent search
        public IActionResult Home(string search, int page = 1, int pageSize = 10)
        {
            var pendingStudents = GetPendingStudents();
            var allStudents = GetAllStudents();

            var totalPending = pendingStudents.Count;
            var pagedPending = pendingStudents
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            var (studentsWithMajor, agentId, agentName) = SearchStudentsByAgent(allStudents, search);

            var model = new StudentListViewModel
            {
                agentName = agentName,
                agentId = agentId,
                SearchTerm = search,
                Students = studentsWithMajor,

                PendingStudents = pagedPending,
                PendingTotalCount = totalPending,
                CurrentPage = page,
                PageSize = pageSize,

                // ✅ for counter

                // ✅ for dropdown
                Agents = _db.Query<agent>("SELECT * FROM Agents WHERE active = 1").ToList()
            };

            return View("~/Views/En/Admin/Home.cshtml", model);
        }

        [Authorize(Roles = "Admin,Super Admin")]
        // Inside AdminController (or any controller under /En/Admin)
        [Route("En/Admin/StudentInfo")]

        public IActionResult StudentInfo(int? studentId)
        {

            string studentSql = "SELECT * FROM Students WHERE active = 1 AND studentId = @studentId";
            var studentInfo = _db.QueryFirstOrDefault<student>(
                studentSql,
                new { studentId = studentId } // ✅ Pass the parameter here
            );

            // Step 1: Get SQL Server data
            var student = _db.QueryFirstOrDefault<StudentInfoViewModel>(
                @"SELECT 
        studentNameEnglish, 
        studentId, 
nationalId,
studentCode,
s.active,
                 nat.nationalityEnglish AS Nationality,
 c.nationalityEnglish AS Country,
        
        city, 
        studentEmail, 
        studentPhone, 
        studentPicture, 
        st.statusEnglish As Status,
       d.degreeEnglish As Degree, 
        major_no, 
        semesterId ,
 s.approvalCondition,
s.studentPicture,
s.studentProof_of_Identity,
s.studentHigh_School_Certificate,
s.studentHigh_School_Certificate2,
s.studentHigh_School_Certificate3,
s.studentHigh_School_Certificate4,
s.studentHigh_School_Certificate5,
s.studentGrades_Report,
s.studentBachelor_Certification,
s.studentGrades_Report1,
s.CreatedAt,
s.studentApproval
      FROM Students s 
    
            LEFT JOIN Nationalities nat ON s.nationalityId = nat.nationalityId
  LEFT JOIN Nationalities c ON s.countryId=c.nationalityId
LEFT JOIN Degrees d ON s.degreeId=d.degreeId
  LEFT JOIN Statuses st ON s.statusId = st.statusId
      WHERE studentId = @studentId",
                new { studentId = studentId }  // ✅ Make sure the name matches SQL parameter
            );

            if (student == null)
                return null;

            var files = new List<StudentFileViewModel>();

           /* string basePath = "/uploads";*/ // adjust based on your setup

            void AddFile(string title, string filePath, DateTime? uploadDate)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    files.Add(new StudentFileViewModel
                    {
                        Title = title,
                        FileName = Path.GetFileName(filePath),
                        FileUrl = $"{filePath}",
                        UploadedDate = uploadDate// Or fetch actual upload date if available
                    });
                }
            }

            // Add available files
            //AddFile("Picture", student.studentPicture,student.CreatedAt);
            AddFile("Passport / ID / Birth Certificate", student.studentProof_of_Identity, student.CreatedAt);
            AddFile("High School Certificate", student.studentHigh_School_Certificate, student.CreatedAt);
            AddFile("High School Certificate2", student.studentHigh_School_Certificate2, student.CreatedAt);
            AddFile("High School Certificate3", student.studentHigh_School_Certificate3, student.CreatedAt);
            AddFile("High School Certificate4", student.studentHigh_School_Certificate4, student.CreatedAt);
            AddFile("High School Certificate5", student.studentHigh_School_Certificate5, student.CreatedAt);
            AddFile("Academic Transcript(Transfer)", student.studentGrades_Report, student.CreatedAt);
            AddFile("Academic Transcript(Diploma)", student.studentGrades_Report1, student.CreatedAt);
            AddFile("Bachelor's Degree Certificate", student.studentBachelor_Certification, student.CreatedAt);

            // Attach to the view model
            student.Files = files;

            // ✅ Set IsApproved after retrieving the status
            if (student != null)
            {
                student.isApproved = student.studentApproval == 1 ? "Yes" : "No";

                //student.isActive = student.active == 1 ? "Active" : "Inactive";
                student.isActive = student.Status switch
                {
                    "Accepted" => "Accepted",
                    "Accepted with Condition" => "Accepted with Condition",
                    "Rejected" => "Rejected",
                    _ => "Pending"
                };

            }
            // Step 2: Get Major Name and Semester Name from Oracle
            var oracleConnectionString = _configuration.GetConnectionString("OracleConnection");

            using (var oracleConnection = new Oracle.ManagedDataAccess.Client.OracleConnection(oracleConnectionString))
            {
                oracleConnection.Open();

                // Get Major Name (Arabic and English)
                var major = oracleConnection.QueryFirstOrDefault<dynamic>(
      @"SELECT Major_Name_S 
      FROM major_info1_vw 
      WHERE major_no = :major_no",
      new { major_no = studentInfo.major_no }
  );


                if (major != null)
                {
                    student.Major_Name_S = major?.MAJOR_NAME_S?.ToString();
                }
                var faculty = oracleConnection.QueryFirstOrDefault<dynamic>(
        @"SELECT DISTINCT Faculty_Name_S 
      FROM major_info1_vw 
      WHERE Faculty_no = :Faculty_no ",
        new { Faculty_no = studentInfo.Faculty_no }
    );



                if (faculty != null)
                {
                    student.Faculty_Name_S = faculty?.FACULTY_NAME_S?.ToString();
                }
                var semester = studentInfo.semesterId.ToString();
                student.Semester = semester;
                if (semester != null)
                {
                    string semesterValue = student.Semester.ToString();

                    if (semesterValue.Length == 5)
                    {
                        string year = semesterValue.Substring(0, 4);
                        string semesterType = semesterValue.Substring(4, 1);

                        string nextYear = (int.Parse(year) + 1).ToString();

                        string semesterName = semesterType switch
                        {
                            "1" => $"First Semester {year}-{nextYear}",
                            "2" => $"Second Semester {year}-{nextYear}",
                            "3" => $"Summer Semester {year}-{nextYear}",
                            _ => $"Unknown Semester {semesterValue}"
                        };

                        student.Semester = semesterName;
                    }
                }
                //var semester = oracleConnection.Query<CalenderVM>("select * from calendar").FirstOrDefault();
                //student.Semester = semester.SEMESTER.ToString();
                //if (semester != null)
                //{
                //    string semesterValue = student.Semester.ToString();

                //    if (semesterValue.Length == 5)
                //    {
                //        string year = semesterValue.Substring(0, 4);
                //        string semesterType = semesterValue.Substring(4, 1);

                //        string nextYear = (int.Parse(year) + 1).ToString();

                //        string semesterName = semesterType switch
                //        {
                //            "1" => $"First Semester {year}-{nextYear}",
                //            "2" => $"Second Semester {year}-{nextYear}",
                //            "3" => $"Summer Semester {year}-{nextYear}",
                //            _ => $"Unknown Semester {semesterValue}"
                //        };

                //        student.Semester = semesterName;
                //    }
                //}


            }


            return View("~/Views/En/Admin/StudentInfo.cshtml", student);
        }
        //public IActionResult UpdateStatus(int studentId, string newStatus)
        //{
        //    // 1. Get the statusId from Statuses table
        //    var statusId = _db.QueryFirstOrDefault<int?>(
        //        "SELECT statusId FROM Statuses WHERE statusEnglish = @newStatus",
        //        new { newStatus }
        //    );

        //    if (statusId == null)
        //    {
        //        return BadRequest("Invalid status");
        //    }

        //    // 2. Update the student's status
        //    _db.Execute(
        //        "UPDATE Students SET statusId = @statusId WHERE studentId = @studentId",
        //        new { statusId, studentId }
        //    );
        //    TempData["StatusMessage"] = $"Student has been {newStatus.ToLower()} successfully.";
        //    TempData["StatusAction"] = newStatus;  // "Accepted" or "Rejected"

        //    // 3. Redirect back or show message
        //    return RedirectToAction("StudentInfo", new { id = studentId });
        //}
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> UpdateStatus(int studentId, string newStatus, string? approvalCondition, string? rejectionReason)
        {
            // Handle conditional acceptance
            if (newStatus == "Accepted" && !string.IsNullOrWhiteSpace(approvalCondition))
                newStatus = "Accepted with Condition";

            // Get statusId
            var statusId = _db.QueryFirstOrDefault<int?>(
                "SELECT statusId FROM Statuses WHERE statusEnglish = @newStatus",
                new { newStatus }
            );

            if (statusId == null)
                return BadRequest("Invalid status");

            // Prepare SQL and parameters
            string sql;
            object param;

            if (newStatus.StartsWith("Accepted"))
            {
                sql = @"
UPDATE Students 
SET statusId = @statusId, 
    approvalCondition = @approvalCondition, 
    rejectionReason = NULL
WHERE studentId = @studentId";

                param = new { statusId, approvalCondition, studentId };
            }
            else if (newStatus == "Rejected")
            {
                sql = @"
UPDATE Students 
SET statusId = @statusId, 
    rejectionReason = @rejectionReason, 
    approvalCondition = NULL
WHERE studentId = @studentId";

                param = new { statusId, rejectionReason, studentId };
            }
            else
            {
                return BadRequest("Unsupported status");
            }

            _db.Execute(sql, param);

            // ✅ حضّر الريدايركت من الآن
            var redirectResult = RedirectToAction("StudentInfo", new { id = studentId, studentId = studentId });

            // ========= GET AGENT + STUDENT INFO =========
            var info = _db.QueryFirstOrDefault<AgentStudentVM>(@"
SELECT
    a.agentEmail           AS AgentEmail,
    s.studentEmail         AS StudentEmail,
    a.agentNameArabic      AS AgentName,
    a.agentNameEnglish     AS AgentNameEnglish,
    s.studentNameArabic    AS StudentName,
    s.studentNameEnglish   AS StudentNameEnglish,
    s.studentNumber        AS StudentNumber
FROM Students s
LEFT JOIN Agents a ON a.agentId = s.agentId
WHERE s.studentId = @studentId
", new { studentId });

            if (info == null)
                return NotFound("Student not found.");

            // Fallback للأسماء
            string agentDisplayName = !string.IsNullOrWhiteSpace(info.AgentName) ? info.AgentName : (info.AgentNameEnglish ?? "");
            string studentDisplayName = !string.IsNullOrWhiteSpace(info.StudentName) ? info.StudentName : (info.StudentNameEnglish ?? "");
            string studentNo = info.StudentNumber ?? "";

            // ========= SEND EMAIL TO AGENT =========
            if (!string.IsNullOrWhiteSpace(info.AgentEmail))
            {
                try
                {
                    string portalUrl = "https://agent.ammanu.edu.jo/";

                    await SendAgentStudentStatusEmailArAsync(
                        toEmail: info.AgentEmail,
                        agentName: agentDisplayName,
                        studentName: studentDisplayName,
                        studentNumber: studentNo,
                        newStatus: newStatus,
                        approvalCondition: approvalCondition,
                        rejectionReason: rejectionReason,
                        portalUrl: portalUrl
                    );
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Failed to send agent email for studentId={studentId}", studentId);
                }
            }
            else
            {
                // اختياري: لا يوجد بريد وكيل
                // TempData["AgentEmailWarning"] = "لا يوجد بريد إلكتروني للوكيل.";
            }

            // ========= SEND EMAIL TO STUDENT =========
            if (!string.IsNullOrWhiteSpace(info.StudentEmail))
            {
                try
                {
                    await SendStudentStatusEmailArAsync(
                        toEmail: info.StudentEmail,
                        agentName: agentDisplayName,
                        studentName: studentDisplayName,
                        studentNumber: studentNo,
                        newStatus: newStatus,
                        approvalCondition: approvalCondition,
                        rejectionReason: rejectionReason,
                        portalUrl: string.Empty // لا يوجد بوابة طالب
                    );
                }
                catch (Exception ex)
                {
                    // _logger.LogError(ex, "Failed to send student email for studentId={studentId}", studentId);
                }
            }
            else
            {
                // اختياري: لا يوجد بريد طالب
                // TempData["StudentEmailWarning"] = "لا يوجد بريد إلكتروني للطالب.";
            }

            TempData["StatusMessage"] = $"Student has been {newStatus.ToLower()} successfully.";
            TempData["StatusAction"] = newStatus;

            return redirectResult;
        }
        private async Task SendAgentStudentStatusEmailArAsync(
    string toEmail,
    string agentName,
    string studentName,
    string studentNumber,
    string newStatus,          // "Accepted" / "Accepted with Condition" / "Rejected"
    string approvalCondition,  // may be null
    string rejectionReason,    // may be null
    string portalUrl)
        {
            var fromAddress = new MailAddress("hec_info@ammanu.edu.jo", "AAU System");
            var toAddress = new MailAddress(toEmail);

            // الأفضل من config وليس hardcoded
            string fromPassword = "hec123@123";

            string smtpHost = "smtp.office365.com";
            int smtpPort = 587;

            string statusAr =
                newStatus == "Accepted" ? "مقبول" :
                newStatus == "Accepted with Condition" ? "مقبول مع شرط" :
                newStatus == "Rejected" ? "مرفوض" :
                newStatus;

            string extraBlock = "";
            if (newStatus == "Accepted with Condition" && !string.IsNullOrWhiteSpace(approvalCondition))
            {
                extraBlock = $@"
            <tr>
              <td style='padding:10px 0;'>
                <b>الشرط:</b><br/>
                <div style='margin-top:6px;background:#f6f6f8;border:1px solid #eee;padding:10px;border-radius:6px;'>
                    {WebUtility.HtmlEncode(approvalCondition)}
                </div>
              </td>
            </tr>";
            }
            else if (newStatus == "Rejected" && !string.IsNullOrWhiteSpace(rejectionReason))
            {
                extraBlock = $@"
            <tr>
              <td style='padding:10px 0;'>
                <b>سبب الرفض:</b><br/>
                <div style='margin-top:6px;background:#fff4f4;border:1px solid #ffd6d6;padding:10px;border-radius:6px;'>
                    {WebUtility.HtmlEncode(rejectionReason)}
                </div>
              </td>
            </tr>";
            }

            string subject = $"تحديث حالة طلب الطالب - {studentName} ({studentNumber}) - {statusAr}";

            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
</head>
<body style='margin:0;padding:0;background:#f3f3f5;font-family:Tahoma,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px 10px;'>
    <tr>
      <td align='center'>
        <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:10px;overflow:hidden;'>
          
          <tr>
            <td style='background:#413659;padding:16px 20px;' align='center'>
              <img src='https://www.ammanu.edu.jo/media/1bgdv5he/aau-logo.png' alt='AAU' height='50' style='display:block;border:0;outline:none;' />
            </td>
          </tr>

          <tr>
            <td style='padding:18px 20px;text-align:center;'>
              <div style='font-size:22px;font-weight:700;direction:rtl;'>جامعة عمان الأهلية</div>
              <div style='font-size:14px;color:#666;margin-top:4px;'>Al-Ahliyya Amman University</div>
            </td>
          </tr>

          <tr>
            <td style='padding:0 28px 24px 28px;direction:rtl;text-align:right;line-height:1.8;font-size:16px;color:#111;'>
              
              <div>السيد/السيدة <b>{WebUtility.HtmlEncode(agentName)}</b> المحترم/ة،</div>
              <div style='margin-top:8px;'>نود إعلامكم بأنه تم تحديث حالة طلب الطالب/الطالبة التالي:</div>

              <table width='100%' cellpadding='0' cellspacing='0' style='margin-top:14px;'>
                <tr>
                  <td style='padding:6px 0;'><b>اسم الطالب:</b> {WebUtility.HtmlEncode(studentName)}</td>
                </tr>
                <tr>
                  <td style='padding:6px 0;'><b>الرقم الجامعي:</b> {WebUtility.HtmlEncode(studentNumber)}</td>
                </tr>
                <tr>
                  <td style='padding:6px 0;'>
                    <b>الحالة:</b>
                    <span style='display:inline-block;padding:4px 10px;border-radius:999px;background:#f0f0f5;border:1px solid #eee;font-weight:700;'>
                      {WebUtility.HtmlEncode(statusAr)}
                    </span>
                  </td>
                </tr>

                {extraBlock}

                <tr>
                  <td style='padding:18px 0 6px 0;'>
                    <a href='{portalUrl}' style='display:inline-block;padding:10px 14px;border-radius:8px;background:#413659;color:#fff;text-decoration:none;font-weight:700;'>
                      فتح بوابة الوكيل
                    </a>
                  </td>
                </tr>
              </table>

              <div style='margin-top:14px;color:#666;font-size:13px;'>
                مع التحية،<br/>فريق جامعة عمان الأهلية
              </div>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using (var smtp = new SmtpClient
            {
                Host = smtpHost,
                Port = smtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            })
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                await smtp.SendMailAsync(message);
            }
        }
        private async Task SendStudentStatusEmailArAsync(
    string toEmail,
    string agentName,
    string studentName,
    string studentNumber,
    string newStatus,          // "Accepted" / "Accepted with Condition" / "Rejected"
    string approvalCondition,  // may be null
    string rejectionReason,    // may be null
    string portalUrl)
        {
            var fromAddress = new MailAddress("hec_info@ammanu.edu.jo", "AAU System");
            var toAddress = new MailAddress(toEmail);

            // الأفضل من config وليس hardcoded
            string fromPassword = "hec123@123";

            string smtpHost = "smtp.office365.com";
            int smtpPort = 587;

            string statusAr =
                newStatus == "Accepted" ? "مقبول" :
                newStatus == "Accepted with Condition" ? "مقبول مع شرط" :
                newStatus == "Rejected" ? "مرفوض" :
                newStatus;

            string extraBlock = "";
            if (newStatus == "Accepted with Condition" && !string.IsNullOrWhiteSpace(approvalCondition))
            {
                extraBlock = $@"
        <tr>
          <td style='padding:10px 0;'>
            <b>الشرط:</b><br/>
            <div style='margin-top:6px;background:#f6f6f8;border:1px solid #eee;padding:10px;border-radius:6px;'>
              {WebUtility.HtmlEncode(approvalCondition)}
            </div>
          </td>
        </tr>";
            }
            else if (newStatus == "Rejected" && !string.IsNullOrWhiteSpace(rejectionReason))
            {
                extraBlock = $@"
        <tr>
          <td style='padding:10px 0;'>
            <b>سبب الرفض:</b><br/>
            <div style='margin-top:6px;background:#fff4f4;border:1px solid #ffd6d6;padding:10px;border-radius:6px;'>
              {WebUtility.HtmlEncode(rejectionReason)}
            </div>
          </td>
        </tr>";
            }

            // ✅ Subject جديد بدون اسم/رقم الطالب
            string subject = $"تحديث حالة طلبك - {statusAr}";

            // لا توجد بوابة للطالب حالياً، لذلك لا نستخدم الرابط
            _ = portalUrl;
            _ = studentNumber;

            string body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
</head>
<body style='margin:0;padding:0;background:#f3f3f5;font-family:Tahoma,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px 10px;'>
    <tr>
      <td align='center'>
        <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:10px;overflow:hidden;'>

          <tr>
            <td style='background:#413659;padding:16px 20px;' align='center'>
              <img src='https://www.ammanu.edu.jo/media/1bgdv5he/aau-logo.png' alt='AAU' height='50' style='display:block;border:0;outline:none;' />
            </td>
          </tr>

          <tr>
            <td style='padding:18px 20px;text-align:center;'>
              <div style='font-size:22px;font-weight:700;direction:rtl;'>جامعة عمان الأهلية</div>
              <div style='font-size:14px;color:#666;margin-top:4px;'>Al-Ahliyya Amman University</div>
            </td>
          </tr>

          <tr>
            <td style='padding:0 28px 24px 28px;direction:rtl;text-align:right;line-height:1.8;font-size:16px;color:#111;'>
              
              <!-- ✅ ذكر الاسم مرة واحدة فقط (في التحية) -->
              <div>عزيزي/عزيزتي <b>{WebUtility.HtmlEncode(studentName)}</b>،</div>

              <div style='margin-top:10px;'>
                نود إعلامكم بأنه تم تحديث حالة طلبكم لدى الجامعة، وذلك بعد متابعة الوكيل:
                <b>{WebUtility.HtmlEncode(agentName)}</b>.
              </div>

              <table width='100%' cellpadding='0' cellspacing='0' style='margin-top:14px;'>
                <tr>
                  <td style='padding:6px 0;'>
                    <b>الحالة الحالية:</b>
                    <span style='display:inline-block;padding:4px 10px;border-radius:999px;background:#f0f0f5;border:1px solid #eee;font-weight:700;'>
                      {WebUtility.HtmlEncode(statusAr)}
                    </span>
                  </td>
                </tr>

                {extraBlock}
              </table>

              <div style='margin-top:14px;color:#666;font-size:13px;'>
                للاستفسار يرجى التواصل مع وكيلكم المذكور أعلاه.<br/><br/>
                مع التحية،<br/>فريق جامعة عمان الأهلية
              </div>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>";

            using (var smtp = new SmtpClient
            {
                Host = smtpHost,
                Port = smtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            })
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                await smtp.SendMailAsync(message);
            }
        }


        //[Authorize(Roles = "Admin,Super Admin")]
        //public IActionResult UpdateStatus(int studentId, string newStatus, string? approvalCondition, string? rejectionReason)
        //{
        //    // Handle conditional acceptance
        //    if (newStatus == "Accepted" && !string.IsNullOrWhiteSpace(approvalCondition))
        //    {
        //        newStatus = "Accepted with Condition";
        //    }

        //    // Get the actual statusId based on the final status
        //    var statusId = _db.QueryFirstOrDefault<int?>(
        //        "SELECT statusId FROM Statuses WHERE statusEnglish = @newStatus",
        //        new { newStatus }
        //    );

        //    if (statusId == null)
        //    {
        //        return BadRequest("Invalid status");
        //    }

        //    // Prepare SQL and parameters
        //    string sql;
        //    object param;

        //    if (newStatus.StartsWith("Accepted"))
        //    {
        //        sql = @"
        //    UPDATE Students 
        //    SET statusId = @statusId, 
        //        approvalCondition = @approvalCondition, 
        //        rejectionReason = NULL
        //    WHERE studentId = @studentId";

        //        param = new { statusId, approvalCondition, studentId };
        //    }
        //    else if (newStatus == "Rejected")
        //    {
        //        sql = @"
        //    UPDATE Students 
        //    SET statusId = @statusId, 
        //        rejectionReason = @rejectionReason, 
        //        approvalCondition = NULL
        //    WHERE studentId = @studentId";

        //        param = new { statusId, rejectionReason, studentId };
        //    }
        //    else
        //    {
        //        return BadRequest("Unsupported status");
        //    }

        //    _db.Execute(sql, param);

        //    TempData["StatusMessage"] = $"Student has been {newStatus.ToLower()} successfully.";
        //    TempData["StatusAction"] = newStatus;

        //    return RedirectToAction("StudentInfo", new { id = studentId });
        //}

        private string SaveFile(IFormFile file, string fieldName, string nationalId, int? agentId)
        {
            if (file == null || file.Length == 0) return null;

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{nationalId}_{agentId}_{fieldName}{extension}";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return "/uploads/" + fileName;
        }
        private void ValidateUniqueFields(AgentViewModel model)
        {
            bool nationalIdExists = _db.QueryFirstOrDefault<int>(
                "SELECT COUNT(1) FROM Agents WHERE nationalId = @nationalId",
                new { model.nationalId }
            ) > 0;

            if (nationalIdExists)
            {
                ModelState.AddModelError("nationalId", "This National ID is already registered.");
            }

            bool emailExists = _db.QueryFirstOrDefault<int>(
                "SELECT COUNT(1) FROM Agents WHERE agentEmail = @agentEmail",
                new { model.agentEmail }
            ) > 0;

            if (emailExists)
            {
                ModelState.AddModelError("agentEmail", "This Email is already registered.");
            }

            bool codeExists = _db.QueryFirstOrDefault<int>(
               "SELECT COUNT(1) FROM Agents WHERE agentCode = @agentCode",
               new { model.agentCode }
           ) > 0;

            if (codeExists)
            {
                ModelState.AddModelError("agentCode", "This Code is already registered.");
            }

        }
        //without whatsapp and theres error This SqlTransaction has completed; it is no longer usable.
        //[RequestSizeLimit(50_000_000)] // 50 MB
        //[RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
        //[Authorize(Roles = "Admin,Super Admin")]
        //public IActionResult AddAgent(AgentViewModel model)
        //{
        //    if (Request.Method == "GET")
        //    {

        //        string generatedPassword = GenerateRandomPassword();
        //        // Generate a random password
        //        model.passowrd = GenerateRandomPassword(10); // You can choose length

        //        // Clear model state before any logic
        //        ModelState.Clear();
        //        PopulateDropdowns(model);
        //        return View("~/Views/En/Admin/AddAgent.cshtml", model);

        //    }

        //    if (Request.Method == "POST")
        //    {

        //        ValidateUniqueFields(model);
        //        if (!ModelState.IsValid)
        //        {
        //            var errorFields = ModelState
        //             .Where(kvp => kvp.Value.Errors.Count > 0)
        //             .Select(kvp => GetFriendlyFieldName(kvp.Key))
        //             .ToList();

        //            ViewBag.ErrorMessage = "Please correct the following fields: " + string.Join(", ", errorFields);
        //            PopulateDropdowns(model);
        //            return View("~/Views/En/Admin/AddAgent.cshtml", model);
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            _db.Open(); // Ensure the connection is open

        //            using (var transaction = _db.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // Step 1: Generate userNameEnglish from agentNameEnglish by removing spaces
        //                    //string userNameEnglish = model.agentNameEnglish.Replace(" ", "").ToLower();

        //                    // Step 2: Insert into Users table

        //                    string generatedPassword = GenerateRandomPassword();
        //                    model.passowrd = generatedPassword;
        //                    string insertUserSql = @"
        //        INSERT INTO Users (userName, userEmail, userPassword, userTypeId, active)
        //        VALUES (@userName, @userEmail, @userPassword, @userTypeId, 1);
        //        SELECT CAST(SCOPE_IDENTITY() as int);";

        //                    int newUserId = _db.ExecuteScalar<int>(insertUserSql, new
        //                    {
        //                        userName = model.agentCode,
        //                        userEmail = model.agentEmail,
        //                        userPassword = model.passowrd,
        //                        userTypeId = 3
        //                    }, transaction);
        //                    string contractPath = model.agentContract != null ? SaveFile(model.agentContract, "agentContract", model.nationalId, model.agentId) : model.agentContractPath;
        //                    // Step 3: Insert into Agents table
        //                    string insertAgentSql = @"
        //        INSERT INTO Agents 
        //        (agentNameEnglish, agentNameArabic,agentCode, nationalityId, nationalId, city, countryId, agentEmail, agentPhone, commission, contractStartDate, contractEndDate, userId, active,agentStatus,agentContract)
        //        VALUES 
        //        (@agentNameEnglish,@agentNameArabic, @agentCode, @nationalityId, @nationalId, @city, @countryId, @agentEmail, @agentPhone, @commission, @contractStartDate, @contractEndDate, @userId, 1,'Active',@agentContract);";

        //                    _db.Execute(insertAgentSql, new
        //                    {
        //                        model.agentNameEnglish,
        //                        model.agentNameArabic,
        //                        model.agentCode,
        //                        model.nationalId,
        //                        model.nationalityId,
        //                        model.city,
        //                        model.countryId,
        //                        model.agentEmail,
        //                        model.agentPhone,
        //                        model.commission,
        //                        model.contractStartDate,
        //                        model.contractEndDate,
        //                        userId = newUserId,
        //                        agentContract = contractPath
        //                    }, transaction);

        //                    transaction.Commit();
        //                    string loginUrl = GenerateLoginUrl();
        //                    SendAgentCredentialsEmail(model.agentEmail, model.agentCode, model.passowrd, loginUrl);
        //                    ViewBag.SuccessMessage = "Agent added successfully!";
        //                }
        //                catch (Exception ex)
        //                {
        //                    transaction.Rollback();
        //                    ViewBag.ErrorMessage = "Error: " + ex.Message;
        //                }
        //                finally
        //                {
        //                    _db.Close(); // Always close it at the end
        //                }
        //            }

        //            PopulateDropdowns(model);
        //            return View("~/Views/En/Admin/AddAgent.cshtml", model);
        //        }
        //    }
        //    else
        //    {
        //        PopulateDropdowns(model);
        //        return View("~/Views/En/Admin/AddAgent.cshtml", model);

        //    }

        //    return View("~/Views/En/Admin/AddAgent.cshtml", model);

        //}



        //without whatsapp fixeing the error This SqlTransaction has completed; it is no longer usable.
        [RequestSizeLimit(50_000_000)] // 50 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult AddAgent(AgentViewModel model)
        {
            if (Request.Method == "GET")
            {
                model.passowrd = GenerateRandomPassword(10);

                ModelState.Clear();
                PrepareAddAgentView(model);
                return View("~/Views/En/Admin/AddAgent.cshtml", model);
            }

            if (Request.Method != "POST")
            {
                PrepareAddAgentView(model);
                return View("~/Views/En/Admin/AddAgent.cshtml", model);
            }

            // Keep phone normalization if you already added the new phone inputs
            if (!PhoneNumberHelper.TryNormalizeToE164(
                    model.agentPhoneCountryIso2,
                    model.agentPhoneNational,
                    out string normalizedPhone,
                    out string phoneError))
            {
                ModelState.AddModelError("agentPhoneNational", phoneError);
            }
            else
            {
                model.agentPhone = normalizedPhone;
            }

            ValidateUniqueFields(model);

            if (!ModelState.IsValid)
            {
                var errorFields = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .Select(kvp => GetFriendlyFieldName(kvp.Key))
                    .ToList();

                ViewBag.ErrorMessage = "Please correct the following fields: " + string.Join(", ", errorFields);

                PrepareAddAgentView(model);
                return View("~/Views/En/Admin/AddAgent.cshtml", model);
            }

            string postSaveWarning = null;
            string loginUrl = null;

            _db.Open();

            try
            {
                using (var transaction = _db.BeginTransaction())
                {
                    try
                    {
                        string generatedPassword = GenerateRandomPassword();
                        model.passowrd = generatedPassword;

                        string insertUserSql = @"
                    INSERT INTO Users (userName, userEmail, userPassword, userTypeId, active)
                    VALUES (@userName, @userEmail, @userPassword, @userTypeId, 1);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                        int newUserId = _db.ExecuteScalar<int>(insertUserSql, new
                        {
                            userName = model.agentCode,
                            userEmail = model.agentEmail,
                            userPassword = model.passowrd,
                            userTypeId = 3
                        }, transaction);

                        string contractPath = model.agentContract != null
                            ? SaveFile(model.agentContract, "agentContract", model.nationalId, model.agentId)
                            : model.agentContractPath;

                        string insertAgentSql = @"
                    INSERT INTO Agents 
                    (agentNameEnglish, agentNameArabic, agentCode, nationalityId, nationalId, city, countryId, agentEmail, agentPhone, commission, contractStartDate, contractEndDate, userId, active, agentStatus, agentContract)
                    VALUES 
                    (@agentNameEnglish, @agentNameArabic, @agentCode, @nationalityId, @nationalId, @city, @countryId, @agentEmail, @agentPhone, @commission, @contractStartDate, @contractEndDate, @userId, 1, 'Active', @agentContract);";

                        _db.Execute(insertAgentSql, new
                        {
                            model.agentNameEnglish,
                            model.agentNameArabic,
                            model.agentCode,
                            model.nationalId,
                            model.nationalityId,
                            model.city,
                            model.countryId,
                            model.agentEmail,
                            agentPhone = model.agentPhone,
                            model.commission,
                            model.contractStartDate,
                            model.contractEndDate,
                            userId = newUserId,
                            agentContract = contractPath
                        }, transaction);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }

                // OUTSIDE transaction
                loginUrl = GenerateLoginUrl();

                try
                {
                    SendAgentCredentialsEmail(model.agentEmail, model.agentCode, model.passowrd, loginUrl);
                }
                catch (Exception emailEx)
                {
                    postSaveWarning = "Agent saved successfully, but Email failed: " + emailEx.Message;
                }

                ViewBag.SuccessMessage = "Agent added successfully!";
                if (!string.IsNullOrWhiteSpace(postSaveWarning))
                    ViewBag.ErrorMessage = postSaveWarning;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error: " + ex.Message;
            }
            finally
            {
                _db.Close();
            }

            PrepareAddAgentView(model);
            return View("~/Views/En/Admin/AddAgent.cshtml", model);
        }

      

        //with whatsapp
        //[RequestSizeLimit(50_000_000)] // 50 MB
        //[RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
        //[Authorize(Roles = "Admin,Super Admin")]
        //public async Task<IActionResult> AddAgent(AgentViewModel model)
        //{
        //    if (Request.Method == "GET")
        //    {
        //        model.passowrd = GenerateRandomPassword(10);

        //        ModelState.Clear();
        //        PrepareAddAgentView(model);

        //        return View("~/Views/En/Admin/AddAgent.cshtml", model);
        //    }

        //    if (Request.Method == "POST")
        //    {
        //        // Build final phone number from country + local input
        //        if (!PhoneNumberHelper.TryNormalizeToE164(
        //                model.agentPhoneCountryIso2,
        //                model.agentPhoneNational,
        //                out string normalizedPhone,
        //                out string phoneError))
        //        {
        //            ModelState.AddModelError("agentPhoneNational", phoneError);
        //        }
        //        else
        //        {
        //            model.agentPhone = normalizedPhone;
        //        }

        //        // Your existing uniqueness validation
        //        ValidateUniqueFields(model);

        //        if (!ModelState.IsValid)
        //        {
        //            var errorFields = ModelState
        //                .Where(kvp => kvp.Value.Errors.Count > 0)
        //                .Select(kvp => GetFriendlyFieldName(kvp.Key))
        //                .ToList();

        //            ViewBag.ErrorMessage = "Please correct the following fields: " + string.Join(", ", errorFields);

        //            PrepareAddAgentView(model);
        //            return View("~/Views/En/Admin/AddAgent.cshtml", model);
        //        }

        //        _db.Open();

        //        string postSaveWarning = null;

        //        using (var transaction = _db.BeginTransaction())
        //        {
        //            try
        //            {
        //                // Generate password
        //                string generatedPassword = GenerateRandomPassword();
        //                model.passowrd = generatedPassword;

        //                // Insert into Users
        //                string insertUserSql = @"
        //            INSERT INTO Users (userName, userEmail, userPassword, userTypeId, active)
        //            VALUES (@userName, @userEmail, @userPassword, @userTypeId, 1);
        //            SELECT CAST(SCOPE_IDENTITY() as int);";

        //                int newUserId = _db.ExecuteScalar<int>(insertUserSql, new
        //                {
        //                    userName = model.agentCode,
        //                    userEmail = model.agentEmail,
        //                    userPassword = model.passowrd,
        //                    userTypeId = 3
        //                }, transaction);

        //                // Save contract file
        //                string contractPath = model.agentContract != null
        //                    ? SaveFile(model.agentContract, "agentContract", model.nationalId, model.agentId)
        //                    : model.agentContractPath;

        //                // Insert into Agents
        //                string insertAgentSql = @"
        //            INSERT INTO Agents 
        //            (agentNameEnglish, agentNameArabic, agentCode, nationalityId, nationalId, city, countryId, agentEmail, agentPhone, commission, contractStartDate, contractEndDate, userId, active, agentStatus, agentContract)
        //            VALUES 
        //            (@agentNameEnglish, @agentNameArabic, @agentCode, @nationalityId, @nationalId, @city, @countryId, @agentEmail, @agentPhone, @commission, @contractStartDate, @contractEndDate, @userId, 1, 'Active', @agentContract);";

        //                _db.Execute(insertAgentSql, new
        //                {
        //                    model.agentNameEnglish,
        //                    model.agentNameArabic,
        //                    model.agentCode,
        //                    model.nationalId,
        //                    model.nationalityId,
        //                    model.city,
        //                    model.countryId,
        //                    model.agentEmail,
        //                    agentPhone = model.agentPhone, // final international number
        //                    model.commission,
        //                    model.contractStartDate,
        //                    model.contractEndDate,
        //                    userId = newUserId,
        //                    agentContract = contractPath
        //                }, transaction);

        //                transaction.Commit();

        //                string loginUrl = GenerateLoginUrl();

        //                // Send email after successful save
        //                try
        //                {
        //                    SendAgentCredentialsEmail(model.agentEmail, model.agentCode, model.passowrd, loginUrl);
        //                }
        //                catch (Exception emailEx)
        //                {
        //                    postSaveWarning = "Agent saved successfully, but Email failed: " + emailEx.Message;
        //                }

        //                // WhatsApp TEST using the phone entered in the form
        //                try
        //                {
        //                    await _whatsAppService.SendTestMessageAsync(model.agentPhone);
        //                    //await _whatsAppService.SendAgentCredentialsWhatsAppAsync(model.agentPhone,model.agentNameEnglish,model.agentCode,model.userPassword,loginUrl);
        //                }
        //                catch (Exception waEx)
        //                {
        //                    if (string.IsNullOrWhiteSpace(postSaveWarning))
        //                        postSaveWarning = "Agent saved successfully, but WhatsApp failed: " + waEx.Message;
        //                    else
        //                        postSaveWarning += " | WhatsApp failed: " + waEx.Message;
        //                }

        //                ViewBag.SuccessMessage = "Agent added successfully!";
        //                if (!string.IsNullOrWhiteSpace(postSaveWarning))
        //                    ViewBag.ErrorMessage = postSaveWarning;
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                ViewBag.ErrorMessage = "Error: " + ex.Message;
        //            }
        //            finally
        //            {
        //                _db.Close();
        //            }
        //        }

        //        PrepareAddAgentView(model);
        //        return View("~/Views/En/Admin/AddAgent.cshtml", model);
        //    }

        //    PrepareAddAgentView(model);
        //    return View("~/Views/En/Admin/AddAgent.cshtml", model);
        //}
        private void PrepareAddAgentView(AgentViewModel model)
        {
            PopulateDropdowns(model);
            model.PhoneCountries = PhoneCountryService.GetPhoneCountries();
        }

        [HttpGet]
        [Authorize(Roles = "Agent,Super Admin,Admin")]
        public IActionResult PrintRegularEnrollment(int studentId)
        {
            var model = GetStudentById(studentId, new StudentViewModel());

            if (model == null || model.studentId == 0)
                return NotFound();

            model.Nationalities = _db.Query<nationality>(
                "SELECT nationalityId, nationalityArabic FROM Nationalities WHERE active = 1"
            ).ToList();

            model.Countries = _db.Query<country>(
                "SELECT countryId, countryArabic FROM Countries WHERE active = 1"
            ).ToList();

            model.Agents = _db.Query<agent>(
                "SELECT agentId, agentNameArabic FROM Agents WHERE active = 1"
            ).ToList();

            model.Faculties = _oracleDb.Query<FacultyWithProgramsViewModel>(@"
    SELECT Faculty_no, Faculty_Name, Faculty_Name_S
    FROM major_info1_vw
").ToList();

            var programs = _oracleDb.Query<ProgramVM>(@"
    SELECT major_no, MAJOR_NAME, Major_Name_S, degree_code, faculty_no
    FROM major_info1_vw
").ToList();

            foreach (var faculty in model.Faculties)
            {
                faculty.Programs = programs
                    .Where(p => p.faculty_no == faculty.Faculty_no)
                    .ToList();
            }

            return View("~/Views/Ar/PrintRegularEnrollment.cshtml", model);
        }
        private StudentViewModel GetStudentById(int studentId, StudentViewModel model)
        {
            string sql = @"
    SELECT 
        *
    FROM Students
    WHERE studentId = @studentId";

            // Fetch the student data from the database
            var student = _db.QueryFirstOrDefault<student>(sql, new { studentId });

            if (student != null)
            {
                model.studentId = student.studentId;

                model.studentNumber = student.studentNumber;
                model.Password = student.Password;


                model.studentNameArabic = student.studentNameArabic;
                model.studentNameEnglish = student.studentNameEnglish;

                // ===== Arabic names =====
                model.ArabicFirstName = student.ArabicFirstName;
                model.ArabicFatherName = student.ArabicFatherName;
                model.ArabicGrandFatherName = student.ArabicGrandFatherName;
                model.ArabicFamilyName = student.ArabicFamilyName;

                // ===== English names =====
                model.EnglishFirstName = student.EnglishFirstName;
                model.EnglishFatherName = student.EnglishFatherName;

                model.EnglishGrandFatherName = student.EnglishGrandFatherName;
                model.EnglishFamilyName = student.EnglishFamilyName;

                model.motherName = student.motherName;
                model.countryId0 = student.countryId0;
                model.dateOfBirth = student.dateOfBirth.HasValue
                    ? DateOnly.FromDateTime(student.dateOfBirth.Value)
                    : null;
                model.createdDate = student.createdDate.HasValue
    ? DateOnly.FromDateTime(student.createdDate.Value)
    : null;
                model.expiredDate = student.expiredDate.HasValue
          ? DateOnly.FromDateTime(student.expiredDate.Value)
          : null;


                model.docId = student.docId;
                model.cityAdd = student.cityAdd;

                model.isDisabled = student.isDisabled;
                model.disabilityType = student.disabilityType;

                model.isPreviousAAU = student.isPreviousAAU;
                model.previousStudentId = student.previousStudentId;
                model.previousMajor = student.previousMajor;

                model.schoolBranch = student.schoolBranch;
                model.certificateType = student.certificateType;
                model.certificateYear = student.certificateYear;
                model.seatNumber = student.seatNumber;

                model.countryId1 = student.countryId1;
                model.studentFaculty = student.studentFaculty;
                model.schoolMajor = student.schoolMajor;

                model.certificateYearDip = student.certificateYearDip;
                model.studentDiplomaGPA = student.studentDiplomaGPA;

                model.countryId2 = student.countryId2;
                model.studentUniversity = student.studentUniversity;
                model.admissionType = student.admissionType;

                model.Referrer1Name = student.Referrer1Name;
                model.Referrer1Relation = student.Referrer1Relation;
                model.Referrer1Phone = student.Referrer1Phone;

                model.Referrer2Name = student.Referrer2Name;
                model.Referrer2Relation = student.Referrer2Relation;
                model.Referrer2Phone = student.Referrer2Phone;

                // int (DB) → bool (Model)
                model.LegalDeclaration = student.LegalDeclaration == 1;

                model.countryId3 = student.countryId3;

                // ===== already correct =====
                model.nationalId = student.nationalId;
                model.nationalityId = student.nationalityId;
                model.countryId = student.countryId;
                model.city = student.city;
                model.studentEmail = student.studentEmail;
                model.studentPhone = student.studentPhone;
                model.studentSchool = student.studentSchool;
                model.studentGPA = student.studentGPA;

                model.degreeId = student.degreeId;
                model.Faculty_no = student.Faculty_no;
                model.major_no = student.major_no;

                model.agentId = student.agentId;
                model.isTransfer = student.isTransfer;
                model.isDiploma = student.isDiploma;

                model.studentGender = student.studentGender;
                model.studentCode = student.studentCode;

                // files (paths already stored correctly)
                model.studentPicturePath = student.studentPicture;
                model.studentProof_of_IdentityPath = student.studentProof_of_Identity;
                model.studentHigh_School_CertificatePath = student.studentHigh_School_Certificate;
                model.studentGrades_ReportPath = student.studentGrades_Report;
                model.studentGrades_ReportPath1 = student.studentGrades_Report1;
                model.studentBachelor_CertificationPath = student.studentBachelor_Certification;

                model.discountPercentage = student.discountPercentage;
                model.requiredDiscount = student.requiredDiscount;
            }

            return model;
        }

        private void SendAgentCredentialsEmail(string toEmail, int? agentCode, string password, string loginUrl)
        {
            var fromAddress = new MailAddress("hec_info@ammanu.edu.jo", "AAU System");
            var toAddress = new MailAddress(toEmail);
            const string fromPassword = "hec123@123"; // Or use config
            const string subject = "Your Agent Portal Login Credentials";

            //        string body = $@"
            //    <p>Dear Agent,</p>
            //    <p>You have been successfully registered in the AAU system.</p>
            //    <p><strong>Username:</strong> {agentCode}</p>
            //    <p><strong>Password:</strong> {password}</p>
            //    <p>You can log in to the portal using these credentials.</p>
            // <p><a href='{loginUrl}'>Click here to login</a></p>
            //    <br/>
            //    <p>Regards,<br/>AAU Admin Team</p>
            //";
            string body = $@"
    <div class=""wnVEW""><div class=""VyATD""></div><div class=""w4BZ9""><div role=""document""><div tabindex=""0"" aria-label=""Message body"" class=""T31hC GNqVo allowTextSelection OuGoX""><div visibility=""hidden""><div>
<div dir=""ltr"">
<div id=""x_divRplyFwdMsg"" dir=""ltr""><font style=""font-size:11pt;"" face=""Calibri,sans-serif"" color=""black""><b>From:</b> {fromAddress}<br>

<b>To:</b> {toAddress}<br>

<b>Subject:</b> You have been successfully registered in the Agent AAU system.</font> 
<div aria-hidden=""true"">&nbsp;</div></div>
<div style=""background-color:#F4F4F4;width:100%;height:100%;margin:0;padding:0;""><table style=""margin-bottom:40px;"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
<tbody><tr>
<td bgcolor=""#413659"" align=""center""><table style=""max-width:600px;"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
<tbody><tr>
<td style=""padding:40px 10px;"" valign=""top"" align=""center""><a data-auth=""NotApplicable"" rel=""noopener noreferrer"" target=""_blank"" href=""http://www.ammanu.edu.jo"" title=""http://www.ammanu.edu.jo"" data-linkindex=""0""><img style=""color:white;font-size:18px;font-family:Lato,Helvetica,Arial,sans-serif;display:block;width:40px;text-decoration:none;max-width:40px;border-width:0;line-height:100%;outline:none;min-width:40px;"" alt=""Logo"" border=""0"" height=""40"" src=""https://www.ammanu.edu.jo/media/1bgdv5he/aau-logo.png"" data-imagetype=""External""> </a></td></tr></tbody></table></td></tr>
<tr>
<td style=""padding:0 10px;"" bgcolor=""#413659"" align=""center""><table style=""max-width:600px;"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
<tbody><tr>
<td style=""color:black;font-size:36px;font-family:Lato,Helvetica,Arial,sans-serif;font-weight:900;padding:40px 20px 20px 20px;line-height:48px;"" bgcolor=""white"" valign=""top"" align=""center"">
<h1 style=""font-size:36px;font-weight:900;text-align:center;margin:0;"">Al-Ahliyya Amman University </h1>
<h1 style=""font-size:36px;font-weight:900;text-align:center;margin:0;"">جامعة عمان الأهلية</h1></td></tr></tbody></table></td></tr>
<tr>
<td style=""padding:0 10px;"" bgcolor=""#F3F3F5"" align=""center""><table style=""max-width:600px;"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"">
<tbody><tr>
<td style=""color:black;font-family:Lato,Helvetica,Arial,sans-serif;padding:40px 30px 0 30px;line-height:25px;"" bgcolor=""white"" align=""left"">
    <p><strong>Username:</strong> {agentCode}</p>
     <p><strong>Password:</strong> {password}</p>

<tr>
<td style=""color:#303033;font-size:18px;font-family:Lato,Helvetica,Arial,sans-serif;font-weight:400;padding:20px 30px 40px 30px;line-height:25px;"" bgcolor=""white"" align=""left"">Thank you for registration with us please Login to procced to next step <br>
<p><a href='{loginUrl}'>Click here to login</a></p> </td></tr></tbody></table></td></tr></tbody></table></div></div></div>
</div></div><div class=""g4Y3U""></div></div><div class=""DVtfe""></div></div></div>";

            var smtp = new SmtpClient
            {
                Host = "smtp.office365.com", // e.g., smtp.gmail.com
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                smtp.Send(message);
            }
        }
        private string GenerateLoginUrl()
        {
            return Url.Action("Login", "En", null, Request.Scheme);
        }
        private string GenerateRandomPassword(int length = 8)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var res = new char[length];
            var rnd = new Random();

            for (int i = 0; i < length; i++)
            {
                res[i] = valid[rnd.Next(valid.Length)];
            }

            return new string(res);
        }

        private void PopulateDropdowns(AgentViewModel model)
        {
            model.Nationalities = _db.Query<nationality>("SELECT * FROM Nationalities WHERE active = 1").ToList();
            model.Countries = _db.Query<country>("SELECT * FROM Countries WHERE active = 1").ToList();
            model.PhoneCountries = PhoneCountryService.GetPhoneCountries();
            //model.Degrees = _db.Query<degree>("SELECT * FROM Degrees WHERE active = 1").ToList();



        }
        //public IActionResult StudentList(StudentListViewModel model, string search, string selectedAcademicYear)
        //{
        //    // Fetch academic years for the dropdown
        //    model.AcadimicYears = _academicService.GetAcademicYears();

        //    // Set the selected academic year
        //    model.SelectedAcademicYear = selectedAcademicYear;

        //            // ✅ Step 1: Extract start year
        //            string? startYear = null;  // Initialize as null

        //            if (!string.IsNullOrEmpty(selectedAcademicYear) && selectedAcademicYear.Contains("-") )
        //            {
        //               startYear = selectedAcademicYear.Split('-')[0];

        //            }

        //            // Now you can safely use semesterId (it will be null if nothing was selected)

        //            // ✅ Step 3: Fetch students for this semester
        //            var students = GetStudentsByAcademicYear(startYear);
        //    var filteredStudents = SearchStudentsByNameOrCode(students, search);

        //    // Fetch majors and statuses only once
        //    var majors = _oracleDb.Query<ProgramVM>("SELECT major_no, Major_Name_S FROM major_info1_vw").ToList();
        //    var statuses = _db.Query<status>("SELECT statusId, statusEnglish FROM Statuses WHERE active = 1").ToList();

        //    // Map to ViewModel
        //    model.Students = filteredStudents.Select(s => new StudentWithMajorVM
        //    {
        //        studentId = s.studentId,
        //        studentNameEnglish = s.studentNameEnglish ?? "N/A",
        //        studentPhone = s.studentPhone,
        //        studentCode = s.studentCode ?? "N/A",
        //        Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A",
        //        statusEnglish = statuses.FirstOrDefault(st => st.statusId == s.statusId)?.statusEnglish ?? "N/A",
        //        semesterId = s.semesterId
        //    }).ToList();

        //    return View("~/Views/En/Admin/StudentList.cshtml", model);
        //}
        //        [Authorize(Roles = "Agent,Super Admin,Admin")]
        //        public IActionResult StudentList(int? SelectedSemester,StudentListViewModel model, string? search, string? selectedAcademicYear,int? SelectedCountryId,int? SelectedNationalityId, int page = 1, int pageSize = 10)
        //        {
        //            model.SearchTerm = search;
        //            // 1. Fetch academic years for the dropdown
        //            model.AcadimicYears = _academicService.GetAcademicYears();
        //            model.Semesters = new List<SelectListItem>
        //    {
        //        new SelectListItem { Value = "1", Text = "First" },
        //        new SelectListItem { Value = "2", Text = "Second" },
        //        new SelectListItem { Value = "3", Text = "Summer" }
        //    };
        //            model.SelectedSemester = SelectedSemester;

        //            model.SelectedAcademicYear = selectedAcademicYear;
        //            model.Semesters.ForEach(s =>
        //            {
        //                if (s.Value == SelectedSemester?.ToString())
        //                    s.Selected = true;
        //            });
        //            // 2. Extract start year from academic year string
        //            string? startYear = null;
        //            if (!string.IsNullOrEmpty(selectedAcademicYear) && selectedAcademicYear.Contains("-"))
        //            {
        //                startYear = selectedAcademicYear.Split('-')[0];
        //            }

        //            // 3. Extract agentId from JWT cookie (if user is an agent)
        //            var jwt = Request.Cookies["jwt"];
        //            if (!string.IsNullOrEmpty(jwt))
        //            {
        //                var handler = new JwtSecurityTokenHandler();
        //                var token = handler.ReadJwtToken(jwt);
        //                var userType = token.Claims.FirstOrDefault(c => c.Type == "userType")?.Value;

        //                if (userType == "Agent" && model.agentId == null)
        //                {
        //                    var agentIdClaim = token.Claims.FirstOrDefault(c => c.Type == "agentId");
        //                    if (agentIdClaim != null && int.TryParse(agentIdClaim.Value, out int extractedAgentId))
        //                    {
        //                        model.agentId = extractedAgentId;
        //                    }
        //                }
        //            }

        //            // 4. Get students based on academic year and agent ID (if available)
        //            var students = GetStudentsByAcademicYear(startYear);

        //            // 🔒 Apply agent filter if agentId is set
        //            if (model.agentId != null)
        //            {
        //                students = students.Where(s => s.agentId == model.agentId).ToList();
        //            }
        //            // ✅ NEW: Filter by semester if selected
        //            if (SelectedSemester.HasValue && SelectedSemester.Value != 0)
        //            {
        //                students = students.Where(s => s.semesterId == int.Parse(startYear + SelectedSemester.Value.ToString())).ToList();
        //            }
        //            model.Countries = _db.Query<country>("SELECT countryId, countryEnglish FROM Countries")
        //.Select(c => new SelectListItem { Value = c.countryId.ToString(), Text = c.countryEnglish })
        //.ToList();

        //            model.Nationalities = _db.Query<nationality>("SELECT nationalityId, nationalityEnglish FROM Nationalities")
        //                .Select(n => new SelectListItem { Value = n.nationalityId.ToString(), Text = n.nationalityEnglish })
        //                .ToList();
        //            model.SelectedCountryId = SelectedCountryId;
        //            model.SelectedNationalityId = SelectedNationalityId;
        //            if (SelectedCountryId.HasValue && SelectedCountryId.Value != 0)
        //            {
        //                students = students.Where(s => s.countryId == SelectedCountryId).ToList();
        //            }
        //            if (SelectedNationalityId.HasValue && SelectedNationalityId.Value != 0)
        //            {
        //                students = students.Where(s => s.nationalityId == SelectedNationalityId).ToList();
        //            }
        //            // 5. Filter by search
        //            var filteredStudents = SearchStudentsByNameOrCode(students, search);

        //            // ✅ Step 2: Get total count before pagination
        //            var totalStudents = filteredStudents.Count;
        //            // ✅ Step 3: Apply pagination
        //            var pagedStudents = filteredStudents
        //                                .Skip((page - 1) * pageSize)
        //                                .Take(pageSize)
        //                                .ToList();

        //            // 6. Get additional info for mapping
        //            var majors = _oracleDb.Query<ProgramVM>("SELECT major_no, Major_Name_S FROM major_info1_vw").ToList();
        //            var statuses = _db.Query<status>("SELECT statusId, statusEnglish FROM Statuses WHERE active = 1").ToList();
        //            var countries = _db.Query<country>("SELECT * FROM Countries WHERE active = 1").ToList();
        //            // 7. Map to ViewModel
        //            model.Students = pagedStudents.Select(s => new StudentWithMajorVM
        //            {
        //                studentId = s.studentId,
        //                studentNameEnglish = s.studentNameEnglish ?? "N/A",
        //                studentPhone = s.studentPhone,
        //                studentCode = s.studentCode ?? "N/A",
        //                Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A",
        //                statusEnglish = statuses.FirstOrDefault(st => st.statusId == s.statusId)?.statusEnglish ?? "N/A",
        //                semesterId = s.semesterId,
        //                // ✅ show link only if Accepted or Accepted with Condition
        //                CanShowPreAcceptanceDoc = (s.statusId == 3 || s.statusId == 6),
        //                approvedByStudent = s.studentApproval == 1,
        //                 approvalCondition=s.approvalCondition,
        //                 rejectionReason=s.rejectionReason,
        //                requiredDiscount = s.requiredDiscount,
        //                country = countries.FirstOrDefault(c=>c.countryId==s.countryId)?.countryEnglish ?? "N/A",


        //            }).ToList();

        //            // ✅ Step 6: Set pagination properties
        //            model.StudentTotalCount = totalStudents;
        //            model.CurrentPage = page;
        //            model.PageSize = pageSize;
        //            return View("~/Views/En/Admin/StudentList.cshtml", model);
        //        }

        [Authorize(Roles = "Agent,Super Admin,Admin")]
        public IActionResult StudentList(
    int? SelectedSemester,
    StudentListViewModel model,
    string? search,
    string? selectedAcademicYear,
    int? SelectedCountryId,
    int? SelectedNationalityId,
    int? SelectedStatusId,          // ✅ NEW
    int page = 1,
    int pageSize = 10)
        {
            model.SearchTerm = search;

            // 1) dropdown data
            model.AcadimicYears = _academicService.GetAcademicYears();

            model.Semesters = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "First" },
        new SelectListItem { Value = "2", Text = "Second" },
        new SelectListItem { Value = "3", Text = "Summer" }
    };
            model.SelectedSemester = SelectedSemester;

            model.SelectedAcademicYear = selectedAcademicYear;

            // 2) Extract start year from academic year string
            string? startYear = null;
            if (!string.IsNullOrEmpty(selectedAcademicYear) && selectedAcademicYear.Contains("-"))
                startYear = selectedAcademicYear.Split('-')[0];

            // 3) Extract agentId from JWT cookie (if user is an agent)
            var jwt = Request.Cookies["jwt"];
            if (!string.IsNullOrEmpty(jwt))
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                var userType = token.Claims.FirstOrDefault(c => c.Type == "userType")?.Value;

                if (userType == "Agent" && model.agentId == null)
                {
                    var agentIdClaim = token.Claims.FirstOrDefault(c => c.Type == "agentId");
                    if (agentIdClaim != null && int.TryParse(agentIdClaim.Value, out int extractedAgentId))
                        model.agentId = extractedAgentId;
                }
            }

            // 4) Get students
            var students = GetStudentsByAcademicYear(startYear);

            // 🔒 agent filter
            if (model.agentId != null)
                students = students.Where(s => s.agentId == model.agentId).ToList();

            // semester filter
            if (SelectedSemester.HasValue && SelectedSemester.Value != 0 && !string.IsNullOrEmpty(startYear))
                students = students.Where(s => s.semesterId == int.Parse(startYear + SelectedSemester.Value.ToString())).ToList();

            // countries/nationalities dropdowns
            model.Countries = _db.Query<country>("SELECT countryId, countryEnglish FROM Countries")
                .Select(c => new SelectListItem { Value = c.countryId.ToString(), Text = c.countryEnglish })
                .ToList();

            model.Nationalities = _db.Query<nationality>("SELECT nationalityId, nationalityEnglish FROM Nationalities")
                .Select(n => new SelectListItem { Value = n.nationalityId.ToString(), Text = n.nationalityEnglish })
                .ToList();

            model.SelectedCountryId = SelectedCountryId;
            model.SelectedNationalityId = SelectedNationalityId;

            if (SelectedCountryId.HasValue && SelectedCountryId.Value != 0)
                students = students.Where(s => s.countryId3 == SelectedCountryId).ToList();

            if (SelectedNationalityId.HasValue && SelectedNationalityId.Value != 0)
                students = students.Where(s => s.nationalityId == SelectedNationalityId).ToList();

            // ✅ Load statuses ONCE (used for dropdown + mapping)
            var statuses = _db.Query<status>("SELECT statusId, statusEnglish FROM Statuses WHERE active = 1 ORDER BY statusEnglish").ToList();

            model.Statuses = statuses
                .Select(st => new SelectListItem { Value = st.statusId.ToString(), Text = st.statusEnglish })
                .ToList();

            model.SelectedStatusId = SelectedStatusId;

            // ✅ NEW: status filter
            if (SelectedStatusId.HasValue && SelectedStatusId.Value != 0)
                students = students.Where(s => s.statusId == SelectedStatusId.Value).ToList();

            // 5) search filter
            var filteredStudents = SearchStudentsByNameOrCode(students, search);

            // total + pagination
            var totalStudents = filteredStudents.Count;

            var pagedStudents = filteredStudents
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 6) majors/countries for mapping
            var majors = _oracleDb.Query<ProgramVM>("SELECT major_no, Major_Name_S FROM major_info1_vw").ToList();
            var countries = _db.Query<country>("SELECT * FROM Countries WHERE active = 1").ToList();

            // 7) map
            model.Students = pagedStudents.Select(s => new StudentWithMajorVM
            {
                studentId = s.studentId,
                studentNameEnglish = s.studentNameEnglish ?? "N/A",
                studentPhone = s.studentPhone,
                studentCode = s.studentCode ?? "N/A",
                Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A",
                statusEnglish = statuses.FirstOrDefault(st => st.statusId == s.statusId)?.statusEnglish ?? "N/A",
                semesterId = s.semesterId,
                CanShowPreAcceptanceDoc = (s.statusId == 3 || s.statusId == 6),
                approvedByStudent = s.studentApproval == 1,
                approvalCondition = s.approvalCondition,
                rejectionReason = s.rejectionReason,
                requiredDiscount = s.requiredDiscount,
                country = countries.FirstOrDefault(c => c.countryId == s.countryId3)?.countryEnglish ?? "N/A",
            }).ToList();

            model.StudentTotalCount = totalStudents;
            model.CurrentPage = page;
            model.PageSize = pageSize;

            return View("~/Views/En/Admin/StudentList.cshtml", model);
        }


       

        private static string DegreeToArabic(int? degreeId)
        {
            if (!degreeId.HasValue) return "";

            // عدّل القيم حسب نظامك إذا مختلفة
            switch (degreeId.Value)
            {
                case 2: return "البكالوريوس";
                case 3: return "الدبلوم";
                case 4: return "الماجستير";
                default: return "الدرجة";
            }
        }

        private static string BuildAcademicYear(int semesterId)
        {
            // مثال: 20251 => 2025/2026
            if (semesterId <= 0) return "";
            int year = semesterId / 10;
            return $"{year}/{year + 1}";
        }

        private static string BuildSemesterText(int semesterId)
        {
            if (semesterId <= 0) return "الفصل الدراسي";
            int sem = semesterId % 10;
            if (sem == 1) return "الفصل الأول";
            if (sem == 2) return "الفصل الثاني";
            if (sem == 3) return "الفصل الصيفي";
            return "الفصل الدراسي";
        }

        // ✅ الوثائق من approvalCondition فقط
        private static List<string> BuildDocsFromApprovalCondition(string approvalCondition)
        {
            if (string.IsNullOrWhiteSpace(approvalCondition))
                return new List<string>(); // لا يوجد

            // تقسيم حسب أسطر/فواصل شائعة + الفاصلة العربية
            var docs = approvalCondition
                .Split(new[] { "\r\n", "\n", "\r", "•", "-", "–", ";", "؛", "،" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (docs.Count == 0)
                docs.Add(approvalCondition.Trim());

            return docs;
        }

        [Authorize(Roles = "Agent,Super Admin,Admin")]
        public IActionResult PreliminaryAcceptanceDoc(int studentId)
        {
            var sql = @"
SELECT  s.studentId,
        s.studentNameArabic,
        s.studentNameEnglish,
        s.studentCode,
        s.studentGender,
        s.nationalityId,
        s.degreeId,
                s.Faculty_no,
        s.major_no,
        s.semesterId,
        s.statusId,
        s.approvalCondition
FROM Students s
WHERE s.studentId = @studentId";

            var s = _db.QueryFirstOrDefault<dynamic>(sql, new { studentId });
            if (s == null) return NotFound();

            // ✅ Null-safe conversions
            int statusId = ((int?)s.statusId) ?? 0;
            int semesterId = ((int?)s.semesterId) ?? 0;
            int studentGender = ((int?)s.studentGender) ?? 0;
            int? natId = (int?)s.nationalityId;
            int? majorNo = (int?)s.major_no;
            int? degreeId = (int?)s.degreeId;
            int? facultyNo = (int?)s.Faculty_no;
            // ✅ Only Accepted (3) or Accepted with Condition (6)
            if (statusId != 3 && statusId != 6)
                return Forbid();

            // nationalityArabic (null-safe)
            string nationalityText = "";
            if (natId.HasValue)
            {
                nationalityText = _db.QueryFirstOrDefault<string>(
                    "SELECT nationalityArabic FROM Nationalities WHERE nationalityId = @id",
                    new { id = natId.Value }
                ) ?? "";
            }

            // Major name from Oracle (null-safe)
            // ملاحظة: في Oracle view غالباً الاسم العربي في Major_Name_S
            string majorName = "";
            if (majorNo.HasValue)
            {
                majorName = _oracleDb.QueryFirstOrDefault<string>(
                    "SELECT Major_Name FROM major_info1_vw WHERE major_no = :major_no",
                    new { major_no = majorNo.Value }
                ) ?? "";
            }

            // ✅ Faculty name from Oracle (replace view/column with your real ones)
            string facultyName = "";
            if (facultyNo.HasValue)
            {
                facultyName = _oracleDb.QueryFirstOrDefault<string>(
                    "SELECT Faculty_Name FROM major_info1_vw WHERE faculty_no = :faculty_no",
                    new { faculty_no = facultyNo.Value }
                ) ?? "";
            }

            string approvalCondition = (string)(s.approvalCondition ?? "");

            // ✅ docs ONLY from approvalCondition
            var docs = BuildDocsFromApprovalCondition(approvalCondition);

            // semester + academic year (مطابق فكرة الورد :contentReference[oaicite:2]{index=2})
            string semesterText = BuildSemesterText(semesterId);
            string academicYearText = BuildAcademicYear(semesterId);

            var vm = new PreliminaryAcceptanceDocVM
            {
                StudentId = ((int?)s.studentId) ?? studentId,
                StudentNameArabic = (string)(s.studentNameArabic ?? ""),
                StudentNameEnglish = (string)(s.studentNameEnglish ?? ""),
                //StudentCode = (string)(s.studentCode ?? ""),

                StudentGender = studentGender,
                NationalityText = nationalityText,
                FacultyName = facultyName,
                MajorName = majorName,

                DegreeText = DegreeToArabic(degreeId),

                StatusId = statusId,
                ApprovalCondition = approvalCondition,

                SemesterId = semesterId,
                SemesterText = semesterText,
                AcademicYearText = academicYearText,

                RequiredDocuments = docs,
                LetterDate = DateTime.Today
            };

            return View("~/Views/En/Admin/PreliminaryAcceptanceDoc.cshtml", vm);
        }

        private List<student> GetStudentsByAcademicYear(string selectedAcademicYear)
        {
            // If nothing selected, return all students
            if (string.IsNullOrEmpty(selectedAcademicYear) || selectedAcademicYear == "0")
            {
                return GetAllStudents();
            }

            // Extract the start year from something like "2024-2025"
            string startYear = selectedAcademicYear.Split('-')[0];

            // Get all students where semesterId starts with that year
            return _db.Query<student>(
                @"SELECT * 
          FROM Students
          WHERE CAST(semesterId AS VARCHAR) LIKE @prefix + '%'",
                new { prefix = startYear }
            ).ToList();
        }

        private List<student> SearchStudentsByNameOrCode(List<student> students, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return students;
            }

            var searchLower = search.ToLower();
            return students
                .Where(s =>
                    (!string.IsNullOrEmpty(s.studentNameEnglish) && s.studentNameEnglish.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(s.studentCode) && s.studentCode.ToLower().Contains(searchLower))
                )
                .ToList();
        }
        private List<agent> SearchAgentsByNameOrCode(List<agent> agents, string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return agents;
            }

            var searchLower = search.ToLower();
            return agents
.Where(a =>
    (!string.IsNullOrEmpty(a.agentNameEnglish) && a.agentNameEnglish.ToLower().Contains(searchLower)) ||
    (!string.IsNullOrEmpty(a.agentCode?.ToString()) && a.agentCode.ToString().ToLower().Contains(searchLower))
)
                .ToList();
        }

        private List<agent> GetAgentsByCountry(int? selectedCountryId)
        {
            if (!selectedCountryId.HasValue || selectedCountryId == 0)
            {
                return GetAllAgents(); // Get all agents if no country is selected
            }

            var countryId = selectedCountryId.Value; // Ensure it's an integer
            return _db.Query<agent>(
                @"SELECT * 
        FROM Agents
        WHERE countryId = @countryId", // Parameterized query
                new { countryId } // Pass countryId parameter as an integer
            ).ToList();
        }
        [Authorize(Roles = "Admin,Super Admin")]

        public IActionResult AgentList(AgentViewModel model, string search, int? countryId, int page = 1, int pageSize = 10)
        {
            UpdateExpiredAgents();

            PopulateDropdowns(model);

            // Set the selected country
            model.countryId = countryId;

            // Fetch the filtered list of agents based on selected country and search term
            var agents = GetAgentsByCountry(countryId);

            var filteredAgents = SearchAgentsByNameOrCode(agents, search);
            var countries = _db.Query<country>("SELECT * FROM Countries WHERE active = 1").ToList();
            // Apply pagination on pending students
            var totalAgents = filteredAgents.Count;
            var pagedAgents = filteredAgents
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();
            // Map filtered students to the ViewModel
            model.Agents = pagedAgents.Select(a => new AgentVM
            {
                agentId = a.agentId,
                agentNameEnglish = a.agentNameEnglish ?? "N/A",
                agentPhone = a.agentPhone,
                agentCode = a.agentCode ?? null,
                country = countries.FirstOrDefault(c => c.countryId == a.countryId)?.countryEnglish ?? "N/A",
                city=a.city,
                agentEmail=a.agentEmail,
                agentStatus=a.agentStatus,
        



            }).ToList();

            // 👇 Set pagination values
            model.AgentTotalCount = totalAgents;
            model.CurrentPage = page;
            model.PageSize = pageSize;

            return View("~/Views/En/Admin/AgentList.cshtml",model);
        }
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


        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult Filter(StudentListViewModel model, string selectedAcademicYear, int agentId, string agentName)
        {
            model.agentName = agentName ?? "";
            // Fetch academic years
            model.AcadimicYears = _academicService.GetAcademicYears();

            // If no year is selected, default to the current academic year
            if (string.IsNullOrEmpty(selectedAcademicYear))
            {
                selectedAcademicYear = GetCurrentAcademicYear(); // Implement this method
            }

            model.SelectedAcademicYear = selectedAcademicYear;

            // Fetch students for that academic year and all semesters
            model.FirstSemesterStudents = _studentsBySemester.    
                
                
                GetStudentsBySemester(selectedAcademicYear, 1, agentId);
            model.SecondSemesterStudents = _studentsBySemester.GetStudentsBySemester(selectedAcademicYear, 2, agentId);
            model.SummerSemesterStudents = _studentsBySemester.GetStudentsBySemester(selectedAcademicYear, 3, agentId);
            model.agentName = agentName;

            return View("~/Views/En/Admin/Filter.cshtml", model);
        }
        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult UpdateAgentStatus(int agentId, string status)
        {
            // Get current status and userId
            string sqlSelect = "SELECT agentStatus, userId FROM Agents WHERE agentId = @agentId";
            var result = _db.QueryFirstOrDefault<(string agentStatus, int userId)>(sqlSelect, new { agentId });

            if (result == default)
            {
                // Handle case where agentId is not found
                return NotFound();
            }

            string currentStatus = result.agentStatus;
            int userId = result.userId;
            string newStatus = status;

            // If clicking again on the same status, unfreeze or unblock
            if ((status == "Freezed" && currentStatus == "Freezed") ||
                (status == "Blocked" && currentStatus == "Blocked"))
            {
                newStatus = "Active";
            }

            // Update agent status
            string sqlUpdateAgent = "UPDATE Agents SET agentStatus = @newStatus WHERE agentId = @agentId";
            _db.Execute(sqlUpdateAgent, new { newStatus, agentId });

            // Set user active status: 0 for Freezed/Blocked, 1 for Active
            int userActive = (newStatus == "Freezed" || newStatus == "Blocked") ? 0 : 1;

            // Update user active status
            string sqlUpdateUser = "UPDATE Users SET active = @userActive WHERE userId = @userId";
            _db.Execute(sqlUpdateUser, new { userActive, userId });

            return RedirectToAction("AgentInfo", new { id = agentId });
        }
        [Authorize(Roles = "Admin,Super Admin")]
        [Route("En/Admin/AgentInfo")]
        //[Route("En/Admin/AgentInfo/{id}")]
        public IActionResult AgentInfo(int agentId)
        {

            string studentSql = "SELECT * FROM Agents WHERE active = 1 AND agentId = @agentId";
            var studentInfo = _db.QueryFirstOrDefault<agent>(
                studentSql,
                new { agentId = agentId } // ✅ Pass the parameter here
            );

            // Step 1: Get SQL Server data
            var agent = _db.QueryFirstOrDefault<AgentViewModel>(
                @"SELECT 
        agentNameEnglish, 
        agentId, 
agentCode,
a.active,
                 nat.nationalityEnglish AS Nationality,
 c.nationalityEnglish AS Country,
        
        city, 
        agentEmail, 
       agentPhone,
notes,
passowrd,
agentStatus
      FROM Agents a
    
            LEFT JOIN Nationalities nat ON a.nationalityId = nat.nationalityId
  LEFT JOIN Nationalities c ON a.countryId=c.nationalityId

      WHERE agentId = @agentId",
                new { agentId = agentId }  // ✅ Make sure the name matches SQL parameter
            );

            if (agent == null)
                return null;
            if (agent != null)
            {
                //agent.isApproved = agent.Status == "Student's Approved " ? "Yes" : "No";
                agent.isActive = agent.agentStatus switch
                {
                    "Freezed" => "Freezed",
                    "Blocked" => "Blocked",
                    _ => "Active"
                };

                agent.agentId = agentId;
            }


            return View("~/Views/En/Admin/AgentInfo.cshtml", agent);
        }
        //[Authorize(Roles = "Admin,Super Admin")]
        //public IActionResult EditAgent(AgentViewModel model, int? agentId, int? id)
        //{
        //    agentId ??= id;
        //    if (Request.Method == "GET")
        //    {
        //        if (!agentId.HasValue)
        //        {
        //            return BadRequest("Agent ID is required to edit.");
        //        }

        //        model = GetAgentById(agentId.Value, model);
        //        ModelState.Clear();
        //        PopulateDropdowns(model);
        //        return View("~/Views/En/Admin/EditAgent.cshtml",model); // View name can be "EditStudent"

        //    }

        //    // Handle POST
        //    if (!agentId.HasValue)
        //    {
        //        return BadRequest("Agent ID is required to update.");
        //    }
        //    var sql = @"SELECT * FROM Agents WHERE agentId = @AgentId";
        //    var existingAgent = _db.QueryFirstOrDefault<agent>(sql, new { AgentId = agentId.Value });

        //    if (existingAgent == null)
        //    {
        //        return NotFound("Agent not found.");
        //    }

        //    ValidateUniqueFields(model, existingAgent);

        //    if (model.agentContractPath != null)
        //    {
        //        ModelState.Remove("agentContract");
        //    }
        //    if (!ModelState.IsValid)
        //    {
        //        var errorFields = ModelState
        //           .Where(kvp => kvp.Value.Errors.Count > 0)
        //           .Select(kvp => GetFriendlyFieldName(kvp.Key))
        //           .ToList();

        //        ViewBag.ErrorMessage = "Please correct the following fields: " + string.Join(", ", errorFields);
        //        PopulateDropdowns(model);
        //        return View("~/Views/En/Admin/EditAgent.cshtml", model);
        //    }




        //        UpdateAgent(agentId.Value, model);
        //    string loginUrl = GenerateLoginUrl();
        //    SendAgentCredentialsEmail(model.agentEmail, model.agentCode, model.passowrd, loginUrl);

        //    ViewBag.SuccessMessage = "Agent updated successfully!";
        //        PopulateDropdowns(model);



        //    return View("~/Views/En/Admin/EditAgent.cshtml",model);
        //}
        [Authorize(Roles = "Admin,Super Admin")]
        public IActionResult EditAgent(
            AgentViewModel model,
            int? agentId,
            int? id)
        {
            agentId ??= id;

            // =========================
            // GET
            // =========================
            if (Request.Method == "GET")
            {
                if (!agentId.HasValue)
                {
                    return BadRequest("Agent ID is required to edit.");
                }

                model = GetAgentById(agentId.Value, model);

                // Example:
                // +962791234567
                // Country = JO
                // Local number = 0791234567
                PrepareAgentPhoneForEdit(model);

                PopulateDropdowns(model);

                model.PhoneCountries =
                    PhoneCountryService.GetPhoneCountries();

                // Ensure Razor uses the values prepared above.
                ModelState.Clear();

                return View(
                    "~/Views/En/Admin/EditAgent.cshtml",
                    model);
            }

            // =========================
            // POST
            // =========================
            if (!agentId.HasValue)
            {
                return BadRequest("Agent ID is required to update.");
            }

            const string sql = @"
        SELECT *
        FROM Agents
        WHERE agentId = @AgentId";

            var existingAgent = _db.QueryFirstOrDefault<agent>(
                sql,
                new
                {
                    AgentId = agentId.Value
                });

            if (existingAgent == null)
            {
                return NotFound("Agent not found.");
            }

            ValidateUniqueFields(model, existingAgent);

            // A new contract file is not required when an existing
            // contract path is available.
            if (!string.IsNullOrWhiteSpace(model.agentContractPath))
            {
                ModelState.Remove(nameof(model.agentContract));
            }

            // Validate the local phone based on the selected country.
            // This also generates model.agentPhone in E.164 format.
            ValidateAndNormalizeAgentPhone(model);

            if (!ModelState.IsValid)
            {
                var errorFields = ModelState
                    .Where(item =>
                        item.Value != null &&
                        item.Value.Errors.Count > 0)
                    .Select(item =>
                        GetFriendlyFieldName(item.Key))
                    .Distinct()
                    .ToList();

                ViewBag.ErrorMessage =
                    "Please correct the following fields: " +
                    string.Join(", ", errorFields);

                PopulateDropdowns(model);

                // Required every time the view is returned.
                model.PhoneCountries =
                    PhoneCountryService.GetPhoneCountries();

                return View(
                    "~/Views/En/Admin/EditAgent.cshtml",
                    model);
            }

            UpdateAgent(agentId.Value, model);

            string loginUrl = GenerateLoginUrl();

            SendAgentCredentialsEmail(
                model.agentEmail,
                model.agentCode,
                model.passowrd,
                loginUrl);

            ViewBag.SuccessMessage =
                "Agent updated successfully!";

            // Rebuild the local edit fields from the normalized
            // international number after saving.
            PrepareAgentPhoneForEdit(model);

            PopulateDropdowns(model);

            model.PhoneCountries =
                PhoneCountryService.GetPhoneCountries();

            // Prevent posted values from overriding the updated model.
            ModelState.Clear();

            return View(
                "~/Views/En/Admin/EditAgent.cshtml",
                model);
        }
        private readonly PhoneNumberUtil _phoneNumberUtil =
    PhoneNumberUtil.GetInstance();

        private void PrepareAgentPhoneForEdit(AgentViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.agentPhone))
                return;

            try
            {
                // Existing saved number should be international:
                // Example: +962791234567
                var parsedPhone = _phoneNumberUtil.Parse(
                    model.agentPhone,
                    null);

                if (!_phoneNumberUtil.IsValidNumber(parsedPhone))
                    return;

                var regionCode =
                    _phoneNumberUtil.GetRegionCodeForNumber(parsedPhone);

                if (string.IsNullOrWhiteSpace(regionCode) ||
                    regionCode == "ZZ")
                {
                    return;
                }

                model.agentPhoneCountryIso2 = regionCode;

                // Return the number in local/national format.
                // Example: +962791234567 becomes 0791234567.
                var nationalFormat = _phoneNumberUtil.Format(
                    parsedPhone,
                    PhoneNumberFormat.NATIONAL);

                model.agentPhoneNational = Regex.Replace(
                    nationalFormat,
                    @"[^\d]",
                    "");
            }
            catch (NumberParseException)
            {
                // Support older records that may not be saved in E.164.
                model.agentPhoneNational = model.agentPhone;
            }
        }

        private void ValidateAndNormalizeAgentPhone(
            AgentViewModel model)
        {
            // Do not trust agentPhone from the hidden input.
            // The server will rebuild it.
            ModelState.Remove(nameof(model.agentPhone));

            // Required attributes already handle empty values.
            if (string.IsNullOrWhiteSpace(model.agentPhoneCountryIso2) ||
                string.IsNullOrWhiteSpace(model.agentPhoneNational))
            {
                return;
            }

            string countryIso2 =
                model.agentPhoneCountryIso2.Trim().ToUpperInvariant();

            string localPhone =
                model.agentPhoneNational.Trim();

            if (localPhone.StartsWith("+"))
            {
                ModelState.AddModelError(
                    nameof(model.agentPhoneNational),
                    "Enter the local phone number only without the country calling code.");

                return;
            }

            // Allow spaces, brackets and hyphens, then remove them.
            if (!Regex.IsMatch(localPhone, @"^[0-9\s\-\(\)]+$"))
            {
                ModelState.AddModelError(
                    nameof(model.agentPhoneNational),
                    "Phone number can contain numbers only.");

                return;
            }

            localPhone = Regex.Replace(
                localPhone,
                @"[^\d]",
                "");

            try
            {
                var parsedPhone = _phoneNumberUtil.Parse(
                    localPhone,
                    countryIso2);

                if (!_phoneNumberUtil.IsValidNumber(parsedPhone))
                {
                    ModelState.AddModelError(
                        nameof(model.agentPhoneNational),
                        "The local phone number is not valid for the selected country.");

                    return;
                }

                // Final value saved in the database:
                // Example: +962791234567
                model.agentPhone = _phoneNumberUtil.Format(
                    parsedPhone,
                    PhoneNumberFormat.E164);

                model.agentPhoneCountryIso2 = countryIso2;
                model.agentPhoneNational = localPhone;
            }
            catch (NumberParseException)
            {
                ModelState.AddModelError(
                    nameof(model.agentPhoneNational),
                    "The local phone number is not valid for the selected country.");
            }
        }
        private string GetFriendlyFieldName(string fieldKey)
        {
            return fieldKey switch
            {
                "nationalId" => "National ID",
                "agentEmail" => "Email",
                "agentPhone" => "Phone",

                "countryId" => "Country",
                "nationalityId" => "Nationality",
                _ => fieldKey // fallback to original if no mapping
            };
        }

        private void ValidateUniqueFields(AgentViewModel model, agent existingAgent)
        {
            // Check email only if it changed
            if (!string.Equals(model.agentEmail, existingAgent.agentEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailQuery = @"SELECT COUNT(1) 
                           FROM Agents 
                           WHERE agentEmail = @Email AND agentId != @AgentId";

                var emailExists = _db.ExecuteScalar<int>(emailQuery, new
                {
                    Email = model.agentEmail,
                    AgentId = existingAgent.agentId
                }) > 0;

                if (emailExists)
                {
                    ModelState.AddModelError("agentEmail", "This email is already in use.");
                }
            }

            // Check national ID only if it changed
            if (!string.Equals(model.nationalId, existingAgent.nationalId, StringComparison.OrdinalIgnoreCase))
            {
                var idQuery = @"SELECT COUNT(1) 
                        FROM Agents 
                        WHERE nationalId = @NationalId AND agentId != @AgentId";

                var idExists = _db.ExecuteScalar<int>(idQuery, new
                {
                    NationalId = model.nationalId,
                    AgentId = existingAgent.agentId
                }) > 0;

                if (idExists)
                {
                    ModelState.AddModelError("nationalId", "This National ID is already in use.");
                }
            }
        }
        //    public void UpdateExpiredAgents()
        //    {
        //        var currentDate = DateTime.Now;

        //        string sql = @"
        //    UPDATE Agents 
        //    SET agentStatus = 'Expired'
        //    WHERE contractEndDate < @CurrentDate AND agentStatus != 'Expired'
        //";

        //        _db.Execute(sql, new { CurrentDate = currentDate });
        //    }
        public void UpdateExpiredAgents()
        {
            var currentDate = DateTime.Now;

            string sqlExpired = @"
        UPDATE Agents 
        SET agentStatus = 'Expired'
        WHERE contractEndDate < @CurrentDate AND agentStatus != 'Expired'
    ";

            string sqlActive = @"
        UPDATE Agents 
        SET agentStatus = 'Active'
        WHERE contractEndDate >= @CurrentDate AND agentStatus != 'Active'
    ";

            _db.Execute(sqlExpired, new { CurrentDate = currentDate });
            _db.Execute(sqlActive, new { CurrentDate = currentDate });
        }

        private AgentViewModel GetAgentById(int agentId, AgentViewModel model)
        {

            string sql = @"
   SELECT 
    a.*, 
    u.userPassword 
FROM Agents a
JOIN Users u ON a.userId = u.userId
WHERE a.agentId = @agentId
";

            // Fetch the student data from the database
            var agent = _db.QueryFirstOrDefault<AgentWithUserPassword>(sql, new { agentId });

            if (agent != null)
            {
                model.agentNameEnglish = agent.agentNameEnglish;
                model.agentNameArabic = agent.agentNameArabic;
                model.nationalId = agent.nationalId;
                model.nationalityId = agent.nationalityId;
                model.countryId = agent.countryId;
                model.city = agent.city;
                model.agentEmail = agent.agentEmail;
                model.agentPhone = agent.agentPhone;
                model.agentIban = agent.agentIban;
                model.commission = agent.commission;
                model.contractStartDate = agent.contractStartDate;
                model.contractEndDate = agent.contractEndDate;
                model.passowrd = agent.userPassword;
                model.notes = agent.notes;
                model.agentCode = agent.agentCode;
                model.agentContractPath = agent.agentContract;

            }

            return model;
        }
        private void UpdateAgent(int agentId, AgentViewModel model)
        {
            string contractPath = model.agentContract != null ? SaveFile(model.agentContract, "agentContract", model.nationalId, model.agentId) : model.agentContractPath;

            string sql = @"
        UPDATE Agents SET
            agentNameEnglish = @agentNameEnglish,
            agentNameArabic = @agentNameArabic,

   agentCode=@agentCode,
            nationalId = @nationalId,
            agentEmail = @agentEmail,
            agentPhone = @agentPhone,
        
            city = @city,
            agentIban = @agentIban,
            commission = @commission,
            contractStartDate = @contractStartDate,
            contractEndDate = @contractEndDate,
            nationalityId = @nationalityId,
            countryId = @countryId,
            passowrd = @passowrd,
            notes = @notes,
    agentContract = @agentContract
        WHERE agentId = @agentId";

            var agent = new
            {
                model.agentNameEnglish ,
                model.agentNameArabic,
            model.nationalId ,
            model.nationalityId,
                model.agentCode,
                model.countryId,
            model.city ,
            model.agentEmail,
            model.agentPhone,
            model.agentIban ,
            model.commission,
            model.contractStartDate,
            model.contractEndDate ,
            model.passowrd ,
            model.notes ,
                agentContract = contractPath,
       
            agentId
            };

            _db.Execute(sql, agent);
            // Get userId
            int userId = _db.QueryFirstOrDefault<int>(
                "SELECT userId FROM Agents WHERE agentId = @agentId",
                new { agentId });

            // Update user password
            string updateUserSql = @"
        UPDATE Users
        SET userName= @userName,
userPassword = @userPassword
        WHERE userId = @userId";

            _db.Execute(updateUserSql, new
            {
                userName = model.agentCode,
                userPassword = model.passowrd,
                userId
            });
        }

        public IActionResult DeleteStudent(int id)
        {
            string sql = "UPDATE Students SET active=0 WHERE studentId = @id";
            _db.Execute(sql, new { id });

            TempData["SuccessMessage"] = "Student deleted successfully!";
            return RedirectToAction("StudentList"); // or whatever your list view is
        }

        public IActionResult DeleteAgent(int id)
        {
            string sql = "UPDATE Agents SET active=0 WHERE agentId = @id";
            _db.Execute(sql, new { id });

            TempData["SuccessMessage"] = "Agent deleted successfully!";
            return RedirectToAction("AgentList"); // or whatever your list view is
        }
    }


}
