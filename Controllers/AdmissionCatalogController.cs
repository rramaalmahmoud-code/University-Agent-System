using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using University_Agent_System.Services;

namespace University_Agent_System.Controllers
{
    [Authorize(Roles = "Agent,Super Admin")]
    public class AdmissionCatalogController : Controller
    {
        private readonly IAdmissionMajorService _majorService;

        public AdmissionCatalogController(
            IAdmissionMajorService majorService)
        {
            _majorService = majorService;
        }

        [HttpGet]
        public IActionResult GetMajors(
            int facultyNo,
            int degreeId,
            int semesterId,
            int? selectedAdmissionMajorId = null)
        {
            if (facultyNo <= 0 ||
                degreeId <= 0 ||
                semesterId <= 0)
            {
                return Json(Array.Empty<object>());
            }

            var majors = _majorService.GetStudentMajors(
                facultyNo,
                degreeId,
                semesterId,
                selectedAdmissionMajorId
            );

            var result = majors.Select(major => new
            {
                admissionMajorId = major.AdmissionMajorId,
                oracleMajorNo = major.OracleMajorNo,
                majorNameArabic = major.MajorNameAr,
                majorNameEnglish = major.MajorNameEn,
                facultyNo = major.FacultyNo,
                degreeCode = major.DegreeCode,
                discountPercentage = major.DiscountPercentage,
                isEnabledForAdmission = major.IsEnabledForAdmission
            });

            return Json(result);
        }
    }
}