using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using University_Agent_System.Data;

using University_Agent_System.Models.ViewModel;
using Dapper;
using University_Agent_System.Models;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using University_Agent_System.Models.Oracle;
using System.Diagnostics;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Oracle.ManagedDataAccess.Types;
using University_Agent_System.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Net.Mail;
using System.Net;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;


namespace University_Agent_System.Controllers
{
    public class EnController : Controller
    {
        private readonly IDbConnection _db;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly IAdmissionMajorService _majorService;

        public EnController(
            IDbConnection db,
            IConfiguration configuration,
            IWebHostEnvironment env,
            IAdmissionMajorService majorService)
        {
            _db = db;
            _configuration = configuration;
            _env = env;
            _majorService = majorService;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Home2()
        {
            return View();
        }


        private void PopulateDropdowns(StudentViewModel model)
        {
            model.Nationalities = _db.Query<nationality>("SELECT * FROM Nationalities WHERE active = 1").ToList();
            model.Countries = _db.Query<country>("SELECT * FROM Countries WHERE active = 1").ToList();
            model.Degrees = _db.Query<degree>("SELECT * FROM Degrees WHERE active = 1").ToList();
            model.Agents = _db.Query<agent>("SELECT * FROM Agents where active=1").ToList();

            var oracleConnectionString = _configuration.GetConnectionString("OracleConnection");

            using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(oracleConnectionString))
            {
                connection.Open();
                var faculties = connection.Query<FacultyVM>(
                    "select distinct Faculty_no,Faculty_Name,Faculty_Name_S from major_info1_vw order by Faculty_no"
                ).ToList();

                var facultyWithPrograms = new List<FacultyWithProgramsViewModel>();

                foreach (var faculty in faculties)
                {
                    //  var programs = connection.Query<ProgramVM>(
                    //      @"SELECT DISTINCT major_no, MAJOR_NAME, Major_Name_S, degree_code 
                    //FROM major_info1_vw 
                    //WHERE (degree_code = 2 OR degree_code = 4) AND Faculty_no = :Faculty_no
                    //ORDER BY degree_code",
                    //      new { Faculty_no = faculty.Faculty_no }
                    //  ).ToList();
                    //                var programs = connection.Query<ProgramVM>(
                    //   @"SELECT DISTINCT major_no, major_name, major_name_s, degree_code 
                    //FROM major_info1_vw 
                    //WHERE faculty_no > 0 AND Faculty_no = :Faculty_no",
                    //   new { Faculty_no = faculty.Faculty_no }
                    // ).ToList();
                    var programs = connection.Query<ProgramVM>(
      @"SELECT DISTINCT major_no, major_name, major_name_s, degree_code 
      FROM major_info1_vw 
      WHERE Faculty_no = :Faculty_no
      ORDER BY degree_code",
      new
      {
          Faculty_no = faculty.Faculty_no
      }
  ).ToList();


                    //                    var sql = @"
                    //    SELECT DISTINCT major_no, major_name, major_name_s, degree_code 
                    //    FROM major_info1_vw 
                    //    WHERE Faculty_no = :Faculty_no
                    //";

                    //                    if (model.degreeId > 0) // only in edit
                    //                    {
                    //                        sql += " AND degree_code = :DegreeCode";
                    //                    }

                    //                    sql += " ORDER BY degree_code";

                    //                    var programs = connection.Query<ProgramVM>(
                    //                        sql,
                    //                        new
                    //                        {
                    //                            Faculty_no = faculty.Faculty_no,
                    //                            DegreeCode = model.degreeId
                    //                        }
                    //                    ).ToList();



                    facultyWithPrograms.Add(new FacultyWithProgramsViewModel
                    {
                        Faculty_no = faculty.Faculty_no,
                        Faculty_Name_S = faculty.Faculty_Name_S,
                        Programs = programs
                    });
                }

                model.Faculties = facultyWithPrograms;
                //temporary then we need to make it dynamic
                model.semesterId ??= 20261;

                //var currentSemester = connection.Query<CalenderVM>("select * from calendar").FirstOrDefault();
                //if (currentSemester != null)
                //{
                //    model.semesterId = currentSemester.SEMESTER;
                //}
            }
        }
        public string GetUserType()
        {
            var jwt = Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(jwt)) return null;

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var typeClaim = token.Claims.FirstOrDefault(c => c.Type == "userType");
            return typeClaim?.Value;
        }

        [Authorize(Roles = "Agent,Super Admin")]
        [HttpGet]
        public IActionResult AddStudent()
        {
            var model = new StudentViewModel(); // Always initialized

            // Clear model state before any logic
            ModelState.Clear();

            // Populate dropdowns in all cases, in case the view uses them
            PopulateDropdowns(model);

            // Check expiration after preparing model and view data
            if (User.IsInRole("Agent") && CheckIfAgentExpired())
            {
                ViewBag.IsExpired = true;
                return View(model);
            }

            // Additional conditional model adjustments
            if (model.isTransfer == 0)
            {
                ModelState.Remove("studentGrades_Report");
                ModelState.Remove("countryId2");
                ModelState.Remove("studentUniversity");
                ModelState.Remove("studentUniversity");
            }
            if (model.isDiploma == 0)
            {
                ModelState.Remove("studentGrades_Report1");
                ModelState.Remove("countryId1");
                ModelState.Remove("studentFaculty");
                ModelState.Remove("schoolMajor");
                ModelState.Remove("certificateYearDip");
                ModelState.Remove("studentDiplomaGPA");
            }
            return View(model);
        }
        [Authorize(Roles = "Agent,Super Admin")]
        [HttpPost]
        public IActionResult AddStudent(StudentViewModel model)
        {
            // ===== Step 1 check =====
            if (!User.Identity.IsAuthenticated)
                return Unauthorized("User lost authentication at method start");
            SetAgentIdFromJwt(model);

            // === Validate uniqueness ===
            ValidateUniqueFields(model);
            ModelState.Remove("studentNumber");
            ModelState.Remove("Password");

            if (model.isTransfer == 0 || model.isTransfer == null)
            {
                ModelState.Remove("studentGrades_Report");
                ModelState.Remove("countryId2");
                ModelState.Remove("studentUniversity");
                ModelState.Remove("studentUniversity");
            }

            if (model.isDiploma == 0 || model.isDiploma == null)
            {
                ModelState.Remove("studentGrades_Report1");
                ModelState.Remove("countryId1");
                ModelState.Remove("studentFaculty");
                ModelState.Remove("schoolMajor");
                ModelState.Remove("certificateYearDip");
                ModelState.Remove("studentDiplomaGPA");
            }

            if (model.degreeId == 2 || model.degreeId == 1)
            {
                ModelState.Remove("studentBachelor_Certification");
            }
            // major_no and discount must come from the database,
            // not from values sent by JavaScript.
            ModelState.Remove(nameof(StudentViewModel.major_no));
            ModelState.Remove(nameof(StudentViewModel.discountPercentage));

            if (!model.AdmissionMajorId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(StudentViewModel.AdmissionMajorId),
                    "Please select a program.");
            }
            else if (!model.semesterId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(StudentViewModel.semesterId),
                    "The current semester is not defined.");
            }
            else
            {
                var selectedMajor = _majorService.GetStudentMajor(
                    model.AdmissionMajorId.Value,
                    model.semesterId.Value,
                    allowDisabled: false);

                if (selectedMajor == null)
                {
                    ModelState.AddModelError(
                        nameof(StudentViewModel.AdmissionMajorId),
                        "The selected program is not currently available for admission.");
                }
                else if (
                    model.Faculty_no != selectedMajor.FacultyNo ||
                    model.degreeId != selectedMajor.DegreeCode)
                {
                    ModelState.AddModelError(
                        nameof(StudentViewModel.AdmissionMajorId),
                        "The selected program does not belong to the selected faculty or degree.");
                }
                else
                {
                    // Always use trusted database values.
                    model.Faculty_no = selectedMajor.FacultyNo;
                    model.degreeId = selectedMajor.DegreeCode;
                    model.major_no = selectedMajor.OracleMajorNo;
                    model.discountPercentage =
                        selectedMajor.DiscountPercentage;
                }
            }
            if (!ModelState.IsValid)
            {
                var errorFields = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .Select(kvp => GetFriendlyFieldName(kvp.Key))
                    .ToList();

                ViewBag.ErrorMessage = "Please correct the following fields: " + string.Join(", ", errorFields);

                PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                // === Proceed with saving ===
                var filePaths = SaveStudentFiles(model);

                int newStudentId = AddStudent(
                    model,
                    filePaths.Picture,
                    filePaths.Identity,
                    filePaths.HighSchool,
                     filePaths.HighSchool2,
                filePaths.HighSchool3,
                filePaths.HighSchool4,
                filePaths.HighSchool5,
                    filePaths.Grades,
                    filePaths.Bachelor,
                     filePaths.SecondaryGrades
                );

                string studentCode = $"{newStudentId}_{model.nationalId}";
                UpdateStudentCode(newStudentId, studentCode);
                string agentName = GetAgentFullName(model.agentId) ?? "your agent";

                // === Generate and send approval link ===
                string approvalUrl = GenerateStudentApprovalUrl(newStudentId);
                SendApprovalEmail(model.studentEmail, model.studentNameEnglish, approvalUrl, agentName);



                ViewBag.StudentCode = studentCode;
                ViewBag.SuccessMessage = "Student added successfully!";
            }
            catch (Exception ex)
            {
                return Content("ERROR: " + ex.Message + "<br><br>StackTrace:<br>" + ex.StackTrace);
            }
            PopulateDropdowns(model);
            return View(model);
        }
        public string GetAgentFullName(int? agentId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return db.QueryFirstOrDefault<string>(
                    "SELECT agentNameEnglish FROM Agents WHERE agentId = @AgentId",
                    new { AgentId = agentId });
            }
        }
        private bool CheckIfAgentExpired()
        {
            var agentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(agentId))
                return true;

            string sql = "SELECT agentStatus FROM Agents WHERE userId = @AgentId";

            string agentStatus = _db.QueryFirstOrDefault<string>(sql, new { AgentId = agentId });

            if (agentStatus == null)
                return true;

            return agentStatus == "Expired"; // or agentStatus == "0" if it's stored as string
        }

        private string GetFriendlyFieldName(string fieldKey)
        {
            return fieldKey switch
            {
                "nationalId" => "National ID",
                "studentEmail" => "Email",
                "studentPhone" => "Phone",

                "countryId" => "Country",
                "nationalityId" => "Nationality",
                _ => fieldKey // fallback to original if no mapping
            };
        }
        private void SendApprovalEmail(string toEmail, string studentName, string approvalUrl, string agentName)
        {
            string subject = "AAU University - Please Confirm Your Registration";

            //    string body = $@"
            //<p>Dear {studentName},</p>
            //<p>Your registration has been submitted by  your agent <strong>{agentName}</strong>.</p>
            //<p>Please confirm your registration by clicking the link below:</p>
            //<p><a href='{approvalUrl}'>Click here to approve</a></p>
            //<br/>
            //<p>Thank you,<br/>AAU University Team</p>";
            string body = $@"
    <div class=""wnVEW""><div class=""VyATD""></div><div class=""w4BZ9""><div role=""document""><div tabindex=""0"" aria-label=""Message body"" class=""T31hC GNqVo allowTextSelection OuGoX""><div visibility=""hidden""><div>
<div dir=""ltr"">

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
 <p>Dear {studentName},</p>
            <p>Your registration has been submitted by  your agent <strong>{agentName}</strong>.</p>

<tr>
<td style=""color:#303033;font-size:18px;font-family:Lato,Helvetica,Arial,sans-serif;font-weight:400;padding:20px 30px 40px 30px;line-height:25px;"" bgcolor=""white"" align=""left"">Thank you for registration with us please Login to procced to next step <br>
<p><a href='{approvalUrl}'>Click here to approve</a></p> </td></tr></tbody></table></td></tr></tbody></table></div></div></div>
</div></div><div class=""g4Y3U""></div></div><div class=""DVtfe""></div></div></div>";


            using (var smtp = new SmtpClient("smtp.office365.com") // Change to your SMTP settings
            {
                Port = 587,
                Credentials = new NetworkCredential("hec_info@ammanu.edu.jo", "hec123@123"),
                EnableSsl = true
            })
            {
                var message = new MailMessage("hec_info@ammanu.edu.jo", toEmail, subject, body)
                {
                    IsBodyHtml = true
                };

                smtp.Send(message);
            }
        }

        private void ValidateUniqueFields(StudentViewModel model)
        {
            bool nationalIdExists = _db.QueryFirstOrDefault<int>(
                "SELECT COUNT(1) FROM Students WHERE nationalId = @nationalId",
                new { model.nationalId }
            ) > 0;

            if (nationalIdExists)
            {
                ModelState.AddModelError("nationalId", "This National ID is already registered.");
            }

            bool emailExists = _db.QueryFirstOrDefault<int>(
                "SELECT COUNT(1) FROM Students WHERE studentEmail = @studentEmail",
                new { model.studentEmail }
            ) > 0;

            if (emailExists)
            {
                ModelState.AddModelError("studentEmail", "This Email is already registered.");
            }

            string userType = GetUserType();
            if (userType == "Admin" && !model.agentId.HasValue)
            {
                ModelState.AddModelError("agentId", "Agent is required.");
            }

        }
        private void ValidateUniqueFields(StudentViewModel model, student existingStudent)
        {
            // Check email only if it changed
            if (!string.Equals(model.studentEmail, existingStudent.studentEmail, StringComparison.OrdinalIgnoreCase))
            {
                var emailQuery = @"SELECT COUNT(1) 
                           FROM Students 
                           WHERE studentEmail = @Email AND studentId != @StudentId";

                var emailExists = _db.ExecuteScalar<int>(emailQuery, new
                {
                    Email = model.studentEmail,
                    StudentId = existingStudent.studentId
                }) > 0;

                if (emailExists)
                {
                    ModelState.AddModelError("studentEmail", "This email is already in use.");
                }
            }

            // Check national ID only if it changed
            if (!string.Equals(model.nationalId, existingStudent.nationalId, StringComparison.OrdinalIgnoreCase))
            {
                var idQuery = @"SELECT COUNT(1) 
                        FROM Students 
                        WHERE nationalId = @NationalId AND studentId != @StudentId";

                var idExists = _db.ExecuteScalar<int>(idQuery, new
                {
                    NationalId = model.nationalId,
                    StudentId = existingStudent.studentId
                }) > 0;

                if (idExists)
                {
                    ModelState.AddModelError("nationalId", "This National ID is already in use.");
                }
            }
        }
        [Authorize(Roles = "Agent,Super Admin,Admin")]
        public IActionResult EditStudent(int? studentId, StudentViewModel model)
        {
            // Get logged-in agent ID from claims
            //var agentIdClaim = User.FindFirst("agentId");
            //var userIdClaim = User.FindFirst("userId");
            //if (agentIdClaim == null|| userIdClaim==null)
            //{
            //    return Forbid(); // Not logged in or invalid token
            //}
            //int currentAgentId = int.Parse(agentIdClaim.Value);

            var userIdClaim = User.FindFirst("userId");
            var agentIdClaim = User.FindFirst("agentId");
            if (userIdClaim == null)
            {
                return Forbid(); // No userId means not authenticated
            }

            int? currentAgentId = null;
            if (User.IsInRole("Agent"))
            {
                if (agentIdClaim == null)
                    return Forbid(); // Agents must have an agentId

                currentAgentId = int.Parse(agentIdClaim.Value);
            }
            studentId = (studentId.HasValue && studentId.Value > 0)
? studentId
: (model.studentId > 0 ? model.studentId : (int?)null);
            if (Request.Method == "GET")
            {
                if (!studentId.HasValue || studentId.Value <= 0)
                    return BadRequest("Student ID is required to edit.");

                model = GetStudentById(studentId.Value, model);
                if (User.IsInRole("Agent") && model.agentId != currentAgentId)
                {
                    return Forbid(); // 🚫 Unauthorized access
                }
                ModelState.Clear();
                PopulateDropdowns(model);
                return View(model);
            }

            // Handle POST
            if (!studentId.HasValue)
                return BadRequest("Student ID is required to update.");

            var sql = @"SELECT * FROM Students WHERE studentId = @StudentId";
            var existingStudent = _db.QueryFirstOrDefault<student>(sql, new { StudentId = studentId.Value });

            if (existingStudent == null)
            {
                return NotFound("Student not found.");
            }

            // === Get agentName from agentId ===
            string agentName = null;
            if (existingStudent.agentId != null)
            {
                var agentSql = @"SELECT agentNameEnglish 
                     FROM Agents 
                     WHERE agentId = @AgentId";
                agentName = _db.QueryFirstOrDefault<string>(agentSql, new { AgentId = existingStudent.agentId });
            }
            if (User.IsInRole("Agent") && existingStudent.agentId != currentAgentId)
            {
                return Forbid(); // 🚫 Unauthorized access
            }
            ValidateUniqueFields(model, existingStudent);

            if (model.isTransfer == null || model.isTransfer == 0 || model.studentGrades_ReportPath != null)
            {
                ModelState.Remove("studentGrades_Report");
                ModelState.Remove("countryId2");
                ModelState.Remove("studentUniversity");
                ModelState.Remove("studentUniversity");
            }

            if (model.isDiploma == null || model.isDiploma == 0 || model.studentGrades_ReportPath1 != null)
            {
                ModelState.Remove("studentGrades_Report1");
                ModelState.Remove("countryId1");
                ModelState.Remove("studentFaculty");
                ModelState.Remove("schoolMajor");
                ModelState.Remove("certificateYearDip");
                ModelState.Remove("studentDiplomaGPA");
            }
            if (model.studentProof_of_IdentityPath != null)
            {
                ModelState.Remove("studentProof_of_Identity");
            }
            if (model.studentPicturePath != null)
            {
                ModelState.Remove("studentPicture");
            }
            if (model.studentHigh_School_CertificatePath != null)
            {
                ModelState.Remove("studentHigh_School_Certificate");
            }
            if (model.studentBachelor_CertificationPath != null)
            {
                ModelState.Remove("studentBachelor_Certification");
            }

            if (model.degreeId == 2 || model.degreeId == 1)
            {
                ModelState.Remove("studentBachelor_Certification");
            }

            ModelState.Remove(nameof(StudentViewModel.major_no));
            ModelState.Remove(nameof(StudentViewModel.discountPercentage));

            if (!model.AdmissionMajorId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(StudentViewModel.AdmissionMajorId),
                    "Please select a major.");
            }
            else if (!model.semesterId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(StudentViewModel.semesterId),
                    "The semester is not specified.");
            }
            else
            {
                int? existingAdmissionMajorId = _db.QueryFirstOrDefault<int?>(
                    @"SELECT AdmissionMajorId
                      FROM Students
                      WHERE studentId = @StudentId",
                    new { StudentId = studentId.Value });

                if (!existingAdmissionMajorId.HasValue &&
                    existingStudent.major_no.HasValue &&
                    existingStudent.Faculty_no.HasValue &&
                    existingStudent.degreeId.HasValue)
                {
                    existingAdmissionMajorId = _db.QueryFirstOrDefault<int?>(
                        @"SELECT TOP (1) AdmissionMajorId
                          FROM AdmissionMajors
                          WHERE OracleMajorNo = @OracleMajorNo
                            AND SourceFacultyNo = @FacultyNo
                            AND SourceDegreeCode = @DegreeCode
                          ORDER BY IsEnabledForAdmission DESC,
                                   AdmissionMajorId DESC",
                        new
                        {
                            OracleMajorNo = existingStudent.major_no.Value,
                            FacultyNo = existingStudent.Faculty_no.Value,
                            DegreeCode = existingStudent.degreeId.Value
                        });
                }

                bool allowDisabled =
                    existingAdmissionMajorId.HasValue &&
                    existingAdmissionMajorId.Value == model.AdmissionMajorId.Value;

                var selectedMajor = _majorService.GetStudentMajor(
                    model.AdmissionMajorId.Value,
                    model.semesterId.Value,
                    allowDisabled);

                if (selectedMajor == null)
                {
                    ModelState.AddModelError(
                        nameof(StudentViewModel.AdmissionMajorId),
                        "The selected major is not available for admission.");
                }
                else if (model.Faculty_no != selectedMajor.FacultyNo ||
                         model.degreeId != selectedMajor.DegreeCode)
                {
                    ModelState.AddModelError(
                        nameof(StudentViewModel.AdmissionMajorId),
                        "The selected major does not belong to the selected faculty or degree.");
                }
                else
                {
                    model.Faculty_no = selectedMajor.FacultyNo;
                    model.degreeId = selectedMajor.DegreeCode;
                    model.major_no = selectedMajor.OracleMajorNo;
                    model.discountPercentage = selectedMajor.DiscountPercentage;
                }
            }

            if (!ModelState.IsValid)
            {
                var errorFields = ModelState
                   .Where(kvp => kvp.Value.Errors.Count > 0)
                   .Select(kvp => GetFriendlyFieldName(kvp.Key))
                   .ToList();

                ViewBag.ErrorMessage = "Please correct the following fields: " + string.Join(", ", errorFields);
                PopulateDropdowns(model);
                return View("EditStudent", model);
            }

            SetAgentIdFromJwt(model);
            var filePaths = SaveStudentFiles(model);

            UpdateStudent(studentId.Value, model,
                filePaths.Picture,
                filePaths.Identity,
                filePaths.HighSchool,
                  filePaths.HighSchool2,
                filePaths.HighSchool3,
                filePaths.HighSchool4,
                filePaths.HighSchool5,
                filePaths.Grades,
                filePaths.Bachelor,
                      filePaths.SecondaryGrades);
            string studentCode = $"{studentId.Value}_{model.nationalId}";

            UpdateStudentCode(studentId.Value, studentCode);

            UpdateStatusToPendingIfRejected(studentId.Value);
            // === Generate and send approval link ===
            string approvalUrl = GenerateStudentApprovalUrl(studentId.Value);
            SendApprovalEmail(model.studentEmail, model.studentNameArabic, approvalUrl, agentName);


            //ViewBag.SuccessMessage = "Student information has been successfully updated!";
            //PopulateDropdowns(model);
            //return View("EditStudent", model);


            var refreshedModel = GetStudentById(studentId.Value, new StudentViewModel());
            refreshedModel.studentId = studentId.Value;

            ViewBag.SuccessMessage = "Student information has been successfully updated!";
            PopulateDropdowns(refreshedModel);
            return View("EditStudent", refreshedModel);
        }
        private void UpdateStatusToPendingIfRejected(int studentId)
        {
            const string rejectedStatus = "Rejected";
            const string pendingStatus = "Pending";

            var currentStatus = _db.QueryFirstOrDefault<string>(
                @"SELECT s.statusEnglish 
          FROM Students st 
          JOIN Statuses s ON st.statusId = s.statusId 
          WHERE st.studentId = @studentId",
                new { studentId }
            );

            if (currentStatus == rejectedStatus)
            {
                var pendingStatusId = _db.QueryFirstOrDefault<int>(
                    "SELECT statusId FROM Statuses WHERE statusEnglish = @pendingStatus",
                    new { pendingStatus }
                );

                _db.Execute(
                    "UPDATE Students SET statusId = @statusId WHERE studentId = @studentId",
                    new { statusId = pendingStatusId, studentId }
                );
            }
        }

        private void SetAgentIdFromJwt(StudentViewModel model)
        {
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
                    {
                        model.agentId = extractedAgentId;
                    }
                }
            }
        }
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
        private (string Picture, string Identity, string HighSchool, string HighSchool2, string HighSchool3, string HighSchool4, string HighSchool5, string Grades, string Bachelor, string SecondaryGrades) SaveStudentFiles(StudentViewModel model)
        {
            string picturePath = model.studentPicture != null ? SaveFile(model.studentPicture, "studentPicture", model.nationalId, model.agentId) : model.studentPicturePath;
            string identityPath = model.studentProof_of_Identity != null ? SaveFile(model.studentProof_of_Identity, "studentProof_of_Identity", model.nationalId, model.agentId) : model.studentProof_of_IdentityPath;
            string highSchoolPath = model.studentHigh_School_Certificate != null ? SaveFile(model.studentHigh_School_Certificate, "studentHigh_School_Certificate", model.nationalId, model.agentId) : model.studentHigh_School_CertificatePath;
            string highSchoolPath2 = model.studentHigh_School_Certificate2 != null ? SaveFile(model.studentHigh_School_Certificate2, "studentHigh_School_Certificate2", model.nationalId, model.agentId) : model.studentHigh_School_CertificatePath2;
            string highSchoolPath3 = model.studentHigh_School_Certificate3 != null ? SaveFile(model.studentHigh_School_Certificate3, "studentHigh_School_Certificate3", model.nationalId, model.agentId) : model.studentHigh_School_CertificatePath3;
            string highSchoolPath4 = model.studentHigh_School_Certificate4 != null ? SaveFile(model.studentHigh_School_Certificate4, "studentHigh_School_Certificate4", model.nationalId, model.agentId) : model.studentHigh_School_CertificatePath4;
            string highSchoolPath5 = model.studentHigh_School_Certificate5 != null ? SaveFile(model.studentHigh_School_Certificate5, "studentHigh_School_Certificate5", model.nationalId, model.agentId) : model.studentHigh_School_CertificatePath5;

            string gradesPath = model.studentGrades_Report != null ? SaveFile(model.studentGrades_Report, "studentGrades_Report", model.nationalId, model.agentId) : model.studentGrades_ReportPath;
            string bachelorPath = model.studentBachelor_Certification != null ? SaveFile(model.studentBachelor_Certification, "studentBachelor_Certification", model.nationalId, model.agentId) : model.studentBachelor_CertificationPath;
            string secondaryGradesPath = model.studentGrades_Report1 != null ? SaveFile(model.studentGrades_Report1, "studentSecondaryGrades_Report", model.nationalId, model.agentId) : model.studentGrades_ReportPath1;

            return (picturePath, identityPath, highSchoolPath, highSchoolPath2, highSchoolPath3, highSchoolPath4, highSchoolPath5, gradesPath, bachelorPath, secondaryGradesPath);
        }
        private void UpdateStudentCode(int studentId, string studentCode)
        {
            string updateSql = "UPDATE Students SET studentCode = @studentCode WHERE studentId = @studentId";
            _db.Execute(updateSql, new { studentCode, studentId });
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
                model.semesterId = student.semesterId;

                model.AdmissionMajorId = _db.QueryFirstOrDefault<int?>(
                    @"SELECT AdmissionMajorId
                      FROM Students
                      WHERE studentId = @StudentId",
                    new { StudentId = studentId });

                if (!model.AdmissionMajorId.HasValue &&
                    model.major_no.HasValue &&
                    model.Faculty_no.HasValue &&
                    model.degreeId.HasValue)
                {
                    model.AdmissionMajorId = _db.QueryFirstOrDefault<int?>(
                        @"SELECT TOP (1) AdmissionMajorId
                          FROM AdmissionMajors
                          WHERE OracleMajorNo = @OracleMajorNo
                            AND SourceFacultyNo = @FacultyNo
                            AND SourceDegreeCode = @DegreeCode
                          ORDER BY IsEnabledForAdmission DESC,
                                   AdmissionMajorId DESC",
                        new
                        {
                            OracleMajorNo = model.major_no.Value,
                            FacultyNo = model.Faculty_no.Value,
                            DegreeCode = model.degreeId.Value
                        });
                }

                model.agentId = student.agentId;
                model.isTransfer = student.isTransfer;
                model.isDiploma = student.isDiploma;

                model.studentGender = student.studentGender;
                model.studentCode = student.studentCode;

                // files (paths already stored correctly)
                model.studentPicturePath = student.studentPicture;
                model.studentProof_of_IdentityPath = student.studentProof_of_Identity;
                model.studentHigh_School_CertificatePath = student.studentHigh_School_Certificate;
                model.studentHigh_School_CertificatePath2 = student.studentHigh_School_Certificate2;
                model.studentHigh_School_CertificatePath3 = student.studentHigh_School_Certificate3;
                model.studentHigh_School_CertificatePath4 = student.studentHigh_School_Certificate4;
                model.studentHigh_School_CertificatePath5 = student.studentHigh_School_Certificate5;
                model.studentGrades_ReportPath = student.studentGrades_Report;
                model.studentGrades_ReportPath1 = student.studentGrades_Report1;
                model.studentBachelor_CertificationPath = student.studentBachelor_Certification;

                model.discountPercentage = student.discountPercentage;
                model.requiredDiscount = student.requiredDiscount;
            }

            return model;
        }



        private int AddStudent(StudentViewModel model,
    string studentPicture,
    string identityPath,
    string highSchoolPath,
       string highSchoolPath2,
   string highSchoolPath3,
   string highSchoolPath4,
   string highSchoolPath5,
    string gradesPath,
    string bachelorPath,
   string secondaryGradesPath)

        {
            string sql = @"
       INSERT INTO Students
(
    -- Names
    studentNameArabic,
    ArabicFirstName,
    ArabicFatherName,
    ArabicGrandFatherName,
    ArabicFamilyName,

    studentNameEnglish,
    EnglishFirstName,
 EnglishFatherName,
    EnglishGrandFatherName,
    EnglishFamilyName,

    motherName,

    -- Personal Info
    nationalId,
    dateOfBirth,
    studentGender,
    countryId,
    nationalityId,
    countryId0,
    city,
    cityAdd,

    -- Contact
    studentEmail,
    studentPhone,

    -- School / Certificate
    countryId3,
    studentSchool,
    schoolBranch,
    certificateType,
    certificateYear,
    seatNumber,
    studentGPA,

    -- Diploma
    countryId1,
    studentFaculty,
    schoolMajor,
    certificateYearDip,
    studentDiplomaGPA,

    -- Transfer
    countryId2,
    studentUniversity,
    isTransfer,
    isDiploma,

   -- Academic
faculty_no,
AdmissionMajorId,
major_no,
degreeId,
semesterId,

    -- Previous AAU
    isPreviousAAU,
    previousStudentId,
    previousMajor,

    -- Disability
    isDisabled,
    disabilityType,

    -- Admission
    admissionType,
    agentId,

    -- Referrers
    Referrer1Name,
    Referrer1Relation,
    Referrer1Phone,
    Referrer2Name,
    Referrer2Relation,
    Referrer2Phone,

    -- Files
    studentPicture,
    studentProof_of_Identity,
    studentHigh_School_Certificate,
  studentHigh_School_Certificate2,
    studentHigh_School_Certificate3,
    studentHigh_School_Certificate4,
    studentHigh_School_Certificate5,
    studentGrades_Report,
    studentGrades_Report1,
    studentBachelor_Certification,

    -- System
    createdDate,
    expiredDate,
    docId,
    active,
    statusId,
    studentApproval,
    LegalDeclaration,
    discountPercentage,
    requiredDiscount
)
VALUES
(
    @studentNameArabic,
    @ArabicFirstName,
    @ArabicFatherName,
    @ArabicGrandFatherName,
    @ArabicFamilyName,

@studentNameEnglish,
@EnglishFirstName,
@EnglishFatherName,
@EnglishGrandFatherName,
@EnglishFamilyName,


    @motherName,

    @nationalId,
    @dateOfBirth,
    @studentGender,
    @countryId,
    @nationalityId,
    @countryId0,
    @city,
    @cityAdd,

    @studentEmail,
    @studentPhone,
@countryId3,
    @studentSchool,
    @schoolBranch,
    @certificateType,
    @certificateYear,
    @seatNumber,
    @studentGPA,

    @countryId1,
    @studentFaculty,
    @schoolMajor,
    @certificateYearDip,
    @studentDiplomaGPA,

    @countryId2,
    @studentUniversity,
    @isTransfer,
    @isDiploma,

    @Faculty_no,
@AdmissionMajorId,
@major_no,
@degreeId,
@semesterId,

    @isPreviousAAU,
    @previousStudentId,
    @previousMajor,

    @isDisabled,
    @disabilityType,

    @admissionType,
    @agentId,

    @Referrer1Name,
    @Referrer1Relation,
    @Referrer1Phone,
    @Referrer2Name,
    @Referrer2Relation,
    @Referrer2Phone,

    @studentPicture,
    @studentProof_of_Identity,
    @studentHigh_School_Certificate,
    @studentHigh_School_Certificate2,
    @studentHigh_School_Certificate3,
    @studentHigh_School_Certificate4,
    @studentHigh_School_Certificate5,
    @studentGrades_Report,
    @studentGrades_Report1,
    @studentBachelor_Certification,

    @createdDate,
    @expiredDate,
    @docId,
    1,
    2,
    0,
    @LegalDeclaration,
    @discountPercentage,
    @requiredDiscount
);

SELECT CAST(SCOPE_IDENTITY() AS INT);
";

            var student = new
            {
                model.studentNameArabic,
                //fromhere 
                model.ArabicFirstName,
                model.ArabicFatherName,
                model.ArabicGrandFatherName,
                model.ArabicFamilyName,
                model.EnglishFirstName,
                model.EnglishFatherName,
                model.EnglishGrandFatherName,
                model.EnglishFamilyName,
                model.motherName,
                model.countryId0,
                dateOfBirth = model.dateOfBirth?.ToDateTime(TimeOnly.MinValue),
                createdDate = model.createdDate?.ToDateTime(TimeOnly.MinValue),
                expiredDate = model.expiredDate?.ToDateTime(TimeOnly.MinValue),
                model.docId,
                model.cityAdd,
                model.isDisabled,
                model.disabilityType,
                model.isPreviousAAU,
                model.previousStudentId,
                model.previousMajor,
                model.schoolBranch,
                model.certificateType,
                model.certificateYear,
                model.seatNumber,
                model.countryId1,
                model.studentFaculty,
                model.schoolMajor,
                model.certificateYearDip,
                model.studentDiplomaGPA,
                model.countryId2,
                model.studentUniversity,
                model.admissionType,
                model.Referrer1Name,
                model.Referrer1Relation,
                model.Referrer1Phone,
                model.Referrer2Name,
                model.Referrer2Relation,
                model.Referrer2Phone,
                LegalDeclaration = model.LegalDeclaration ? 1 : 0,
                model.countryId3,
                //to here
                model.studentNameEnglish,
                model.nationalId,
                model.studentEmail,
                model.studentPhone,
                model.city,
                model.studentSchool,
                model.studentGPA,
                model.Faculty_no,
                model.AdmissionMajorId,
                model.major_no,
                model.nationalityId,
                model.countryId,
                model.semesterId,
                model.isTransfer,
                model.isDiploma,
                model.degreeId,
                studentPicture = studentPicture,
                studentProof_of_Identity = identityPath,
                studentHigh_School_Certificate = highSchoolPath,
                studentHigh_School_Certificate2 = highSchoolPath2,
                studentHigh_School_Certificate3 = highSchoolPath3,
                studentHigh_School_Certificate4 = highSchoolPath4,
                studentHigh_School_Certificate5 = highSchoolPath5,
                studentGrades_Report = gradesPath,
                studentGrades_Report1 = secondaryGradesPath,
                studentBachelor_Certification = bachelorPath,
                model.studentGender,
                model.agentId,
                model.discountPercentage,
                model.requiredDiscount
            };

            return _db.ExecuteScalar<int>(sql, student);
        }


        private void UpdateStudent(int studentId, StudentViewModel model,
string studentPicture,
string identityPath,
string highSchoolPath,
    string highSchoolPath2,
    string highSchoolPath3,
    string highSchoolPath4,
    string highSchoolPath5,
string gradesPath,
string bachelorPath,
string secondaryGradesPath)
        {
            string sql = @"
       UPDATE Students SET
    studentNumber        = @studentNumber,
    Password        = @Password,
    studentNameArabic        = @studentNameArabic,
    studentNameEnglish       = @studentNameEnglish,

    ArabicFirstName          = @ArabicFirstName,
    ArabicFatherName         = @ArabicFatherName,
    ArabicGrandFatherName    = @ArabicGrandFatherName,
    ArabicFamilyName         = @ArabicFamilyName,

    EnglishFirstName         = @EnglishFirstName,
    EnglishFatherName         = @EnglishFatherName,

    EnglishGrandFatherName   = @EnglishGrandFatherName,
    EnglishFamilyName        = @EnglishFamilyName,

    motherName               = @motherName,
    nationalId               = @nationalId,
    docId                    = @docId,

    dateOfBirth              = @dateOfBirth,
    createdDate              = @createdDate,
    expiredDate              = @expiredDate,

    studentEmail             = @studentEmail,
    studentPhone             = @studentPhone,
    city                     = @city,
    cityAdd                  = @cityAdd,

    isDisabled               = @isDisabled,
    disabilityType           = @disabilityType,

    isPreviousAAU            = @isPreviousAAU,
    previousStudentId        = @previousStudentId,
    previousMajor            = @previousMajor,

    studentSchool            = @studentSchool,
    schoolBranch             = @schoolBranch,
    schoolMajor              = @schoolMajor,

    certificateType          = @certificateType,
    certificateYear          = @certificateYear,
    seatNumber               = @seatNumber,
    studentGPA               = @studentGPA,

    certificateYearDip       = @certificateYearDip,
    studentDiplomaGPA        = @studentDiplomaGPA,

    Faculty_no               = @Faculty_no,
    AdmissionMajorId         = @AdmissionMajorId,
    major_no                 = @major_no,
    studentFaculty           = @studentFaculty,
    degreeId                 = @degreeId,

    nationalityId            = @nationalityId,
    countryId0               = @countryId0,
    countryId1               = @countryId1,
    countryId2               = @countryId2,
    countryId3               = @countryId3,
    countryId                = @countryId,

    semesterId               = @semesterId,

    isTransfer               = @isTransfer,
    isDiploma                = @isDiploma,

    studentUniversity        = @studentUniversity,
    admissionType            = @admissionType,

    Referrer1Name            = @Referrer1Name,
    Referrer1Relation        = @Referrer1Relation,
    Referrer1Phone           = @Referrer1Phone,

    Referrer2Name            = @Referrer2Name,
    Referrer2Relation        = @Referrer2Relation,
    Referrer2Phone           = @Referrer2Phone,

    LegalDeclaration         = @LegalDeclaration,

    studentPicture           = @studentPicture,
    studentProof_of_Identity = @studentProof_of_Identity,
    studentHigh_School_Certificate = @studentHigh_School_Certificate,
    studentHigh_School_Certificate2 = @studentHigh_School_Certificate2,
    studentHigh_School_Certificate3 = @studentHigh_School_Certificate3,
    studentHigh_School_Certificate4 = @studentHigh_School_Certificate4,
    studentHigh_School_Certificate5 = @studentHigh_School_Certificate5,
    studentGrades_Report     = @studentGrades_Report,
    studentGrades_Report1    = @studentGrades_Report1,
    studentBachelor_Certification = @studentBachelor_Certification,

    studentGender            = @studentGender,
    agentId                  = @agentId,

    discountPercentage       = @discountPercentage,
    requiredDiscount         = @requiredDiscount

WHERE studentId = @studentId;
";

            var student = new
            {
                model.studentNumber,
                model.Password,
                model.studentNameArabic,
                //fromhere 
                model.ArabicFirstName,
                model.ArabicFatherName,
                model.ArabicGrandFatherName,
                model.ArabicFamilyName,
                model.EnglishFirstName,
                model.EnglishFatherName,
                model.EnglishGrandFatherName,
                model.EnglishFamilyName,
                model.motherName,
                model.countryId0,
                dateOfBirth = model.dateOfBirth?.ToDateTime(TimeOnly.MinValue),
                createdDate = model.createdDate?.ToDateTime(TimeOnly.MinValue),
                expiredDate = model.expiredDate?.ToDateTime(TimeOnly.MinValue),
                model.docId,
                model.cityAdd,
                model.isDisabled,
                model.disabilityType,
                model.isPreviousAAU,
                model.previousStudentId,
                model.previousMajor,
                model.schoolBranch,
                model.certificateType,
                model.certificateYear,
                model.seatNumber,
                model.countryId1,
                model.studentFaculty,
                model.schoolMajor,
                model.certificateYearDip,
                model.studentDiplomaGPA,
                model.countryId2,
                model.studentUniversity,
                model.admissionType,
                model.Referrer1Name,
                model.Referrer1Relation,
                model.Referrer1Phone,
                model.Referrer2Name,
                model.Referrer2Relation,
                model.Referrer2Phone,
                LegalDeclaration = model.LegalDeclaration ? 1 : 0,
                model.countryId3,
                //to here
                model.studentNameEnglish,
                model.nationalId,
                model.studentEmail,
                model.studentPhone,
                model.city,
                model.studentSchool,
                model.studentGPA,
                model.Faculty_no,
                model.AdmissionMajorId,
                model.major_no,
                model.nationalityId,
                model.countryId,
                model.semesterId,
                model.isTransfer,
                model.isDiploma,
                model.degreeId,
                studentPicture,
                studentProof_of_Identity = identityPath,
                studentHigh_School_Certificate = highSchoolPath,
                studentHigh_School_Certificate2 = highSchoolPath2,
                studentHigh_School_Certificate3 = highSchoolPath3,
                studentHigh_School_Certificate4 = highSchoolPath4,
                studentHigh_School_Certificate5 = highSchoolPath5,
                studentGrades_Report = gradesPath,
                studentBachelor_Certification = bachelorPath,
                model.studentGender,
                model.agentId,
                studentGrades_Report1 = secondaryGradesPath,
                model.discountPercentage,
                model.requiredDiscount,
                studentId
            };

            _db.Execute(sql, student);
        }

        public IActionResult StudentInfo()
        {
            return View();
        }
        public IActionResult StudentList()
        {
            return View();
        }

        public IActionResult Login(LoginViewModel model)
        {
            user user = null;

            // Step 1: Get user with their UserType using multi-mapping
            if (!string.IsNullOrEmpty(model.Username))
            {
                var sql = @"SELECT u.*, ut.userTypeId, ut.userTypeEnglish 
                    FROM Users u
                    JOIN UserTypes ut ON u.userTypeId = ut.userTypeId
                    WHERE u.userName = @Username";

                user = _db.Query<user, userType, user>(
                    sql,
                    (u, ut) =>
                    {
                        u.UserType = ut;
                        return u;
                    },
                    new { Username = model.Username },
                    splitOn: "userTypeId"
                ).FirstOrDefault();

                // Optional: Pass user type name to view even if model is invalid
                if (user != null)
                {
                    model.UserTypeName = user.UserType.userTypeEnglish;
                }
            }

            // Step 2: Validate form inputs
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    var state = ModelState[key];
                    foreach (var error in state.Errors)
                    {
                        Console.WriteLine($"Error for {key}: {error.ErrorMessage}");
                    }
                }


                //ViewBag.ErrorMessage = "Please enter both username and password.";
                return View(model);
            }

            // Step 3: Check if user exists first
            if (user == null || user.userPassword != model.Password)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View(model);
            }

            // Step 4: Check if the account is active
            if (user.active != 1)
            {
                ViewBag.ErrorMessage = "Your account is currently inactive due to freezing or blocking";
                return View(model);
            }



            // ✅ Step 4: If user is an Agent, ensure they exist in Agents table
            if (user.UserType.userTypeId == 3)
            {
                var agentExists = _db.QueryFirstOrDefault<int?>(
                    "SELECT agentId FROM Agents WHERE userId = @UserId",
                    new { UserId = user.userId });

                if (agentExists == null)
                {
                    ViewBag.ErrorMessage = "Agent account not found. Please contact support.";
                    return View(model);
                }
            }

            // Step 4: Generate and set JWT cookie
            var token = new TokenService(_configuration, _db).GenerateToken(user);

            //Response.Cookies.Append("jwt", token, new CookieOptions
            //{
            //    HttpOnly = true,
            //    Secure = true,
            //    SameSite = SameSiteMode.Strict,
            //    Expires = DateTimeOffset.Now.AddDays(7)
            //});
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // 👈 set true ONLY if using HTTPS
                SameSite = SameSiteMode.Lax, // 👈 allows redirects to still carry the cookie
                Expires = DateTimeOffset.Now.AddDays(7),
                Path = "/",   // allow everywhere
                Domain = null // 👈 don’t force "localhost", browser will scope to current host (IP or localhost)
            });


            // Step 5: Redirect based on user type
            switch (user.UserType.userTypeEnglish)
            {
                case "Super Admin":
                    return RedirectToAction("Home", "AdminEn");
                case "Admin":
                    return RedirectToAction("Home", "AdminEn");
                case "Agent":
                    return RedirectToAction("StudentList", "AdminEn");
                default:
                    return View();
            }
        }



        public IActionResult Logout()
        {
            // Clear the JWT cookie by setting an expired one
            Response.Cookies.Append("jwt", "", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            // Optional: TempData message if you want to show "Logged out" message
            TempData["LogoutMessage"] = "You have been logged out.";

            // Redirect to login or home page
            return RedirectToAction("Login"); // change controller name if needed
        }


        [Authorize(Roles = "Agent")]
        //[Route("En/StudentInfo/{id}")]
        [Route("En/StudentInfo")]
        public IActionResult StudentInfo(int? studentId)
        {
            // Get logged-in agent ID from claims
            var agentIdClaim = User.FindFirst("agentId");
            if (agentIdClaim == null)
            {
                return Forbid(); // Not logged in or invalid token
            }
            int currentAgentId = int.Parse(agentIdClaim.Value);
            string studentSql = "SELECT * FROM Students WHERE active = 1 AND studentId = @studentId";
            var studentInfo = _db.QueryFirstOrDefault<student>(
                studentSql,
                new { studentId = studentId } // ✅ Pass the parameter here
            );
            if (studentInfo == null)
                return NotFound();

            if (User.IsInRole("Agent") && studentInfo.agentId != currentAgentId)
            {
                return Forbid(); // 🚫 Unauthorized access
            }
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
        semesterId,
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


            // ✅ Set IsApproved after retrieving the status
            if (student != null)
            {
                student.isApproved = student.studentApproval == 1 ? "Yes" : "No";
                student.isActive = student.active == 1 ? "Active" : "Inactive";
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

                //var semester = oracleConnection.Query<CalenderVM>("select * from calendar").FirstOrDefault();
                student.Semester = studentInfo.semesterId.ToString();
                if (student.Semester != null)
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


            }


            return View("~/Views/En/StudentInfo.cshtml", student);
        }



        private string GenerateStudentApprovalUrl(int studentId)
        {
            return Url.Action("ApproveStudent", "En", new { id = studentId }, Request.Scheme);
        }
        // Step 1: Show the page with a confirm button

        public IActionResult ApproveStudent(int id)
        {
            if (Request.Method == "GET")
            {
                var student = _db.QueryFirstOrDefault<student>("SELECT * FROM Students WHERE studentId = @id", new { id });
                return View("ApproveStudent", student);
            }// New view with Confirm button
            if (Request.Method == "POST")
            {

                string sql = "UPDATE Students SET studentApproval = 1 WHERE studentId = @id";
                _db.Execute(sql, new { id });

                var student = _db.QueryFirstOrDefault<student>("SELECT * FROM Students WHERE studentId = @id", new { id });
                return View("ApproveStudent", student);
            }
            return View("ApproveStudent");
        }





    }
}