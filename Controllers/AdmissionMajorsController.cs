using System.Data;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Oracle.ManagedDataAccess.Client;
using University_Agent_System.Models.ViewModel;
using University_Agent_System.Services;

namespace University_Agent_System.Controllers
{
    [Authorize(Roles = "Super Admin")]
    public class AdmissionMajorsController : Controller
    {
        private readonly IAdmissionMajorService _majorService;
        private readonly IOracleMajorSyncService _majorSyncService;
        private readonly IDbConnection _db;
        private readonly IConfiguration _configuration;
        private readonly IAdmissionMajorDiscountService
    _discountService;
        public AdmissionMajorsController(
        IAdmissionMajorService majorService,
        IOracleMajorSyncService majorSyncService,
        IAdmissionMajorDiscountService discountService,
        IDbConnection db,
        IConfiguration configuration)
        {
            _majorService = majorService;
            _majorSyncService = majorSyncService;
            _discountService = discountService;
            _db = db;
            _configuration = configuration;
        }

        /*
         * الصفحة الرئيسية:
         * إدارة التخصصات
         */
        [HttpGet]
        public IActionResult Index(int? semesterId)
        {
            try
            {
                int selectedSemester =
                    semesterId ??
                    _db.ExecuteScalar<int?>(
                        @"SELECT MAX(semesterId)
                  FROM Students
                  WHERE semesterId IS NOT NULL"
                    ) ??
                    20261;

                ViewBag.SelectedSemester =
                    selectedSemester;

                var majors = _majorService.GetAll(
                    selectedSemester
                );

                return View(majors);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "حدث خطأ أثناء تحميل التخصصات: " +
                    ex.Message;

                ViewBag.SelectedSemester =
                    semesterId ?? 20261;

                return View(
                    new List<AdmissionMajorAdminViewModel>()
                );
            }
        }
        /*
         * مزامنة التخصصات من Oracle.
         * تظهر كزر داخل صفحة إدارة التخصصات.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sync()
        {
            try
            {
                var result = _majorSyncService.SyncMajors();

                TempData["SuccessMessage"] =
                    "تمت مزامنة التخصصات بنجاح. " +
                    $"عدد تخصصات Oracle: {result.OracleCount}، " +
                    $"المضافة: {result.AddedCount}، " +
                    $"المحدثة: {result.UpdatedCount}، " +
                    $"غير الموجودة حاليًا في Oracle: " +
                    $"{result.MissingFromOracleCount}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "فشلت عملية مزامنة التخصصات: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /*
         * فتح صفحة إضافة تخصص محلي.
         */
        [HttpGet]
        public IActionResult Create(int? semesterId)
        {
            int selectedSemester =
                semesterId ?? 20261;

            var model = new AdmissionMajorFormViewModel
            {
                SemesterId = selectedSemester,
                DiscountPercentage = 0,
                IsEnabledForAdmission = true,
                IsLocalOnly = true
            };

            PopulateFormDropdowns();

            return View(model);
        }
        /*
         * حفظ تخصص محلي جديد.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            AdmissionMajorFormViewModel model)
        {
            model.IsLocalOnly = true;
            model.OracleMajorNo = null;

            if (!ModelState.IsValid)
            {
                PopulateFormDropdowns(
                    model.DegreeCode,
                    model.FacultyNo
                );

                return View(model);
            }

            try
            {
                string changedBy =
                    User.Identity?.Name ?? "Super Admin";

                int newId = _majorService.AddLocalMajor(
                    model,
                    changedBy
                );
                _discountService.SaveDiscount(
    newId,
    model.SemesterId,
    model.DiscountPercentage,
    changedBy
);
                TempData["SuccessMessage"] =
                    $"تمت إضافة التخصص المحلي بنجاح. " +
                    $"رقم السجل: {newId}.";

                return RedirectToAction(
           nameof(Index),
           new
           {
               semesterId = model.SemesterId
           }
       );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "حدث خطأ أثناء إضافة التخصص: " +
                    ex.Message
                );

                PopulateFormDropdowns(
                    model.DegreeCode,
                    model.FacultyNo
                );

                return View(model);
            }
        }

        /*
         * فتح صفحة تعديل التخصص.
         */
        [HttpGet]
        public IActionResult Edit(
        int id,
        int? semesterId)
        {
            int selectedSemester =
                semesterId ?? 20261;

            var model = _majorService.GetById(
                id,
                selectedSemester
            );

            if (model == null)
            {
                return NotFound();
            }

            PopulateFormDropdowns(
                model.DegreeCode,
                model.FacultyNo
            );

            return View(model);
        }

        /*
         * حفظ تعديل التخصص.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            int id,
            AdmissionMajorFormViewModel model)
        {
            if (id != model.AdmissionMajorId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                PopulateFormDropdowns(
                    model.DegreeCode,
                    model.FacultyNo
                );

                return View(model);
            }

            try
            {
                string changedBy =
                    User.Identity?.Name ?? "Super Admin";

                bool updated = _majorService.UpdateMajor(
                    model,
                    changedBy
                );
                _discountService.SaveDiscount(
    model.AdmissionMajorId,
    model.SemesterId,
    model.DiscountPercentage,
    changedBy
);
                if (!updated)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] =
                    "تم تعديل بيانات التخصص بنجاح.";

                return RedirectToAction(
    nameof(Index),
    new
    {
        semesterId = model.SemesterId
    }
);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "حدث خطأ أثناء تعديل التخصص: " +
                    ex.Message
                );

                PopulateFormDropdowns(
                    model.DegreeCode,
                    model.FacultyNo
                );

                return View(model);
            }
        }

        /*
         * حذف التخصص من قائمة القبول.
         *
         * هذا ليس DELETE فعلياً.
         * يتم تحويل IsEnabledForAdmission إلى false.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                string changedBy =
                    User.Identity?.Name ?? "Super Admin";

                bool updated = _majorService.SetMajorStatus(
                    id,
                    false,
                    changedBy
                );

                if (!updated)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] =
                    "تم حذف التخصص من قائمة القبول.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "حدث خطأ أثناء حذف التخصص: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /*
         * إعادة التخصص المحذوف إلى قائمة القبول.
         */
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Restore(int id)
        {
            try
            {
                string changedBy =
                    User.Identity?.Name ?? "Super Admin";

                bool updated = _majorService.SetMajorStatus(
                    id,
                    true,
                    changedBy
                );

                if (!updated)
                {
                    return NotFound();
                }

                TempData["SuccessMessage"] =
                    "تمت استعادة التخصص وإتاحته للقبول.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "حدث خطأ أثناء استعادة التخصص: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        /*
         * تحميل الدرجات العلمية من SQL Server
         * والكليات من Oracle.
         */
        private void PopulateFormDropdowns(
            int? selectedDegreeCode = null,
            int? selectedFacultyNo = null)
        {
            var degrees = _db.Query<LookupItem>(
                @"SELECT
                      degreeId AS Value,
                      degreeArabic AS Text
                  FROM Degrees
                  WHERE active = 1
                  ORDER BY degreeId"
            ).ToList();

            ViewBag.Degrees = new SelectList(
                degrees,
                nameof(LookupItem.Value),
                nameof(LookupItem.Text),
                selectedDegreeCode
            );

            var faculties = GetOracleFaculties();

            ViewBag.Faculties = new SelectList(
                faculties,
                nameof(LookupItem.Value),
                nameof(LookupItem.Text),
                selectedFacultyNo
            );
        }

        /*
         * قراءة الكليات من Oracle.
         */
        private List<LookupItem> GetOracleFaculties()
        {
            string? oracleConnectionString =
                _configuration.GetConnectionString(
                    "OracleConnection"
                );

            if (string.IsNullOrWhiteSpace(
                oracleConnectionString))
            {
                throw new Exception(
                    "OracleConnection غير موجود في appsettings.json"
                );
            }

            using var oracleConnection =
                new OracleConnection(
                    oracleConnectionString
                );

            oracleConnection.Open();

            string sql = @"
                SELECT DISTINCT
                    faculty_no AS ""Value"",
                    faculty_name AS ""Text""
                FROM major_info1_vw
                WHERE faculty_no IS NOT NULL
                  AND faculty_no > 0
                ORDER BY faculty_no";

            return oracleConnection
                .Query<LookupItem>(sql)
                .ToList();
        }

        /*
         * Model داخلي بسيط للقوائم المنسدلة.
         */
        private sealed class LookupItem
        {
            public int Value { get; set; }

            public string Text { get; set; } =
                string.Empty;
        }
    }
}