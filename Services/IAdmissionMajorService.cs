using University_Agent_System.Models.ViewModel;

namespace University_Agent_System.Services
{
    public interface IAdmissionMajorService
    {
        List<AdmissionMajorAdminViewModel> GetAll(
      int semesterId
  );

        AdmissionMajorFormViewModel? GetById(
            int id,
            int semesterId
        );

        int AddLocalMajor(
            AdmissionMajorFormViewModel model,
            string changedBy
        );

        bool UpdateMajor(
            AdmissionMajorFormViewModel model,
            string changedBy
        );

        bool SetMajorStatus(
            int id,
            bool isEnabled,
            string changedBy
        );
        List<StudentMajorOptionViewModel> GetStudentMajors(
    int facultyNo,
    int degreeCode,
    int semesterId,
    int? selectedAdmissionMajorId
);

        StudentMajorOptionViewModel? GetStudentMajor(
            int admissionMajorId,
            int semesterId,
            bool allowDisabled
        );
    }
}