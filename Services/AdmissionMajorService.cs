using System.Data;
using Dapper;
using University_Agent_System.Models.ViewModel;

namespace University_Agent_System.Services
{
    public class AdmissionMajorService : IAdmissionMajorService
    {
        private readonly IDbConnection _db;

        public AdmissionMajorService(IDbConnection db)
        {
            _db = db;
        }

        public List<AdmissionMajorAdminViewModel> GetAll(
          int semesterId)
        {
            string sql = @"
        SELECT
            m.AdmissionMajorId,
            m.OracleMajorNo,

            COALESCE(
                m.OverrideMajorNameAr,
                m.SourceMajorNameAr
            ) AS MajorNameAr,

            COALESCE(
                m.OverrideMajorNameEn,
                m.SourceMajorNameEn
            ) AS MajorNameEn,

            m.SourceMajorNameAr,
            m.SourceMajorNameEn,

            m.SourceDegreeCode AS DegreeCode,
            m.SourceFacultyNo AS FacultyNo,

            COALESCE(
                m.SourceFacultyNameAr,
                facultyNames.FacultyNameAr
            ) AS FacultyNameAr,

            COALESCE(
                m.SourceFacultyNameEn,
                facultyNames.FacultyNameEn
            ) AS FacultyNameEn,

            m.IsLocalOnly,
            m.IsEnabledForAdmission,
            m.ExistsInOracle,
            m.LastOracleSyncAt,

            @SemesterId AS SemesterId,

            d.AdmissionMajorDiscountId,

            CAST(
                ISNULL(d.DiscountPercentage, 0)
                AS DECIMAL(5,2)
            ) AS DiscountPercentage

        FROM AdmissionMajors m

        OUTER APPLY
        (
            SELECT TOP 1
                fm.SourceFacultyNameAr
                    AS FacultyNameAr,

                fm.SourceFacultyNameEn
                    AS FacultyNameEn

            FROM AdmissionMajors fm

            WHERE fm.SourceFacultyNo =
                  m.SourceFacultyNo

              AND
              (
                  fm.SourceFacultyNameAr IS NOT NULL
                  OR
                  fm.SourceFacultyNameEn IS NOT NULL
              )

            ORDER BY
                fm.IsLocalOnly ASC,
                fm.AdmissionMajorId ASC

        ) facultyNames

        LEFT JOIN AdmissionMajorDiscounts d
            ON d.AdmissionMajorId =
               m.AdmissionMajorId

           AND d.SemesterId =
               @SemesterId

           AND d.IsActive = 1

        ORDER BY
            COALESCE(
                m.SourceFacultyNameAr,
                facultyNames.FacultyNameAr
            ),

            m.SourceDegreeCode,

            COALESCE(
                m.OverrideMajorNameAr,
                m.SourceMajorNameAr
            );";

            return _db
                .Query<AdmissionMajorAdminViewModel>(
                    sql,
                    new
                    {
                        SemesterId = semesterId
                    }
                )
                .ToList();
        }
        public AdmissionMajorFormViewModel? GetById(
            int id,
            int semesterId)
        {
            string sql = @"
        SELECT
            m.AdmissionMajorId,
            m.OracleMajorNo,

            COALESCE(
                m.OverrideMajorNameAr,
                m.SourceMajorNameAr
            ) AS MajorNameAr,

            COALESCE(
                m.OverrideMajorNameEn,
                m.SourceMajorNameEn
            ) AS MajorNameEn,

            m.SourceMajorNameAr,
            m.SourceMajorNameEn,

            m.SourceDegreeCode AS DegreeCode,
            m.SourceFacultyNo AS FacultyNo,

            m.IsLocalOnly,
            m.IsEnabledForAdmission,

            @SemesterId AS SemesterId,

            CAST(
                ISNULL(d.DiscountPercentage, 0)
                AS DECIMAL(5,2)
            ) AS DiscountPercentage

        FROM AdmissionMajors m

        LEFT JOIN AdmissionMajorDiscounts d
            ON d.AdmissionMajorId =
               m.AdmissionMajorId
           AND d.SemesterId = @SemesterId
           AND d.IsActive = 1

        WHERE m.AdmissionMajorId = @Id;";

            return _db.QueryFirstOrDefault
                <AdmissionMajorFormViewModel>(
                    sql,
                    new
                    {
                        Id = id,
                        SemesterId = semesterId
                    }
                );
        }
        public int AddLocalMajor(
            AdmissionMajorFormViewModel model,
            string changedBy)
        {
            string sql = @"
                INSERT INTO AdmissionMajors
                (
                    OracleMajorNo,

                    SourceMajorNameAr,
                    SourceMajorNameEn,

                    OverrideMajorNameAr,
                    OverrideMajorNameEn,

                    SourceDegreeCode,
                    SourceFacultyNo,

                    IsLocalOnly,
                    IsEnabledForAdmission,
                    ExistsInOracle,

                    CreatedAt,
                    CreatedBy
                )
                VALUES
                (
                    NULL,

                    NULL,
                    NULL,

                    @MajorNameAr,
                    @MajorNameEn,

                    @DegreeCode,
                    @FacultyNo,

                    1,
                    @IsEnabledForAdmission,
                    0,

                    SYSDATETIME(),
                    @ChangedBy
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return _db.ExecuteScalar<int>(
                sql,
                new
                {
                    MajorNameAr = model.MajorNameAr.Trim(),
                    MajorNameEn = model.MajorNameEn.Trim(),
                    model.DegreeCode,
                    model.FacultyNo,
                    model.IsEnabledForAdmission,
                    ChangedBy = changedBy
                }
            );
        }

        public bool UpdateMajor(
            AdmissionMajorFormViewModel model,
            string changedBy)
        {
            /*
             * التخصص الرسمي:
             * نعدل أسماء Override فقط ولا نغيّر بيانات Oracle الأصلية.
             *
             * التخصص المحلي:
             * يمكن تعديل الاسم والكلية والدرجة.
             */
            string sql = @"
                UPDATE AdmissionMajors
                SET
                    OverrideMajorNameAr = @MajorNameAr,
                    OverrideMajorNameEn = @MajorNameEn,

                    SourceDegreeCode =
                        CASE
                            WHEN IsLocalOnly = 1
                                THEN @DegreeCode
                            ELSE SourceDegreeCode
                        END,

                    SourceFacultyNo =
                        CASE
                            WHEN IsLocalOnly = 1
                                THEN @FacultyNo
                            ELSE SourceFacultyNo
                        END,

                    IsEnabledForAdmission =
                        @IsEnabledForAdmission,

                    UpdatedAt = SYSDATETIME(),
                    UpdatedBy = @ChangedBy

                WHERE AdmissionMajorId =
                    @AdmissionMajorId;";

            int affectedRows = _db.Execute(
                sql,
                new
                {
                    model.AdmissionMajorId,

                    MajorNameAr =
                        model.MajorNameAr.Trim(),

                    MajorNameEn =
                        model.MajorNameEn.Trim(),

                    model.DegreeCode,
                    model.FacultyNo,
                    model.IsEnabledForAdmission,

                    ChangedBy = changedBy
                }
            );

            return affectedRows > 0;
        }

        public bool SetMajorStatus(
            int id,
            bool isEnabled,
            string changedBy)
        {
            /*
             * لا نحذف السجل فعلياً.
             * فقط نمنع ظهوره في شاشة تقديم الطلب.
             */
            string sql = @"
                UPDATE AdmissionMajors
                SET
                    IsEnabledForAdmission = @IsEnabled,
                    UpdatedAt = SYSDATETIME(),
                    UpdatedBy = @ChangedBy
                WHERE AdmissionMajorId = @Id;";

            int affectedRows = _db.Execute(
                sql,
                new
                {
                    Id = id,
                    IsEnabled = isEnabled,
                    ChangedBy = changedBy
                }
            );

            return affectedRows > 0;
        }

        public List<StudentMajorOptionViewModel> GetStudentMajors(
        int facultyNo,
        int degreeCode,
        int semesterId,
        int? selectedAdmissionMajorId)
        {
            string sql = @"
        SELECT
            m.AdmissionMajorId,
            m.OracleMajorNo,

            COALESCE(
                m.OverrideMajorNameAr,
                m.SourceMajorNameAr
            ) AS MajorNameAr,

            COALESCE(
                m.OverrideMajorNameEn,
                m.SourceMajorNameEn
            ) AS MajorNameEn,

            m.SourceFacultyNo AS FacultyNo,
            m.SourceDegreeCode AS DegreeCode,

            CAST(
                ISNULL(d.DiscountPercentage, 0)
                AS DECIMAL(5,2)
            ) AS DiscountPercentage,

            m.IsEnabledForAdmission

        FROM AdmissionMajors m

        LEFT JOIN AdmissionMajorDiscounts d
            ON d.AdmissionMajorId =
               m.AdmissionMajorId

           AND d.SemesterId =
               @SemesterId

           AND d.IsActive = 1

        WHERE m.SourceFacultyNo = @FacultyNo
          AND m.SourceDegreeCode = @DegreeCode
          AND
          (
              m.IsEnabledForAdmission = 1
              OR
              m.AdmissionMajorId =
              @SelectedAdmissionMajorId
          )

        ORDER BY
            COALESCE(
                m.OverrideMajorNameAr,
                m.SourceMajorNameAr
            );";

            return _db.Query<StudentMajorOptionViewModel>(
                sql,
                new
                {
                    FacultyNo = facultyNo,
                    DegreeCode = degreeCode,
                    SemesterId = semesterId,
                    SelectedAdmissionMajorId =
                        selectedAdmissionMajorId
                }
            ).ToList();
        }

        public StudentMajorOptionViewModel? GetStudentMajor(
            int admissionMajorId,
            int semesterId,
            bool allowDisabled)
        {
            string sql = @"
        SELECT
            m.AdmissionMajorId,
            m.OracleMajorNo,

            COALESCE(
                m.OverrideMajorNameAr,
                m.SourceMajorNameAr
            ) AS MajorNameAr,

            COALESCE(
                m.OverrideMajorNameEn,
                m.SourceMajorNameEn
            ) AS MajorNameEn,

            m.SourceFacultyNo AS FacultyNo,
            m.SourceDegreeCode AS DegreeCode,

            CAST(
                ISNULL(d.DiscountPercentage, 0)
                AS DECIMAL(5,2)
            ) AS DiscountPercentage,

            m.IsEnabledForAdmission

        FROM AdmissionMajors m

        LEFT JOIN AdmissionMajorDiscounts d
            ON d.AdmissionMajorId =
               m.AdmissionMajorId

           AND d.SemesterId =
               @SemesterId

           AND d.IsActive = 1

        WHERE m.AdmissionMajorId =
              @AdmissionMajorId

          AND
          (
              m.IsEnabledForAdmission = 1
              OR @AllowDisabled = 1
          );";

            return _db.QueryFirstOrDefault
                <StudentMajorOptionViewModel>(
                    sql,
                    new
                    {
                        AdmissionMajorId =
                            admissionMajorId,

                        SemesterId = semesterId,

                        AllowDisabled =
                            allowDisabled ? 1 : 0
                    }
                );
        }
    }
}