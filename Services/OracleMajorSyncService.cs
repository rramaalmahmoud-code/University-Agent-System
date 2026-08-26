using Dapper;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using University_Agent_System.Models.Admission;

namespace University_Agent_System.Services
{
    public class OracleMajorSyncService : IOracleMajorSyncService
    {
        private readonly IConfiguration _configuration;

        public OracleMajorSyncService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MajorSyncResult SyncMajors()
        {
            var result = new MajorSyncResult();

            string? oracleConnectionString =
                _configuration.GetConnectionString("OracleConnection");

            string? sqlConnectionString =
                _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(oracleConnectionString))
            {
                throw new Exception(
                    "OracleConnection was not found in appsettings.json."
                );
            }

            if (string.IsNullOrWhiteSpace(sqlConnectionString))
            {
                throw new Exception(
                    "DefaultConnection was not found in appsettings.json."
                );
            }

            /*
             * أولاً: قراءة Oracle كاملة.
             *
             * نفعل ذلك قبل تعديل SQL Server حتى لا نجعل جميع
             * التخصصات ExistsInOracle = 0 إذا فشل اتصال Oracle.
             */
            List<OracleMajorDto> oracleMajors;

            using (var oracleConnection =
                   new OracleConnection(oracleConnectionString))
            {
                oracleConnection.Open();

                string oracleQuery = @"
    SELECT
        major_no AS ""OracleMajorNo"",
        MAX(major_name) AS ""SourceMajorNameAr"",
        MAX(major_name_s) AS ""SourceMajorNameEn"",
        MAX(degree_code) AS ""SourceDegreeCode"",
        MAX(faculty_no) AS ""SourceFacultyNo"",
        MAX(faculty_name) AS ""SourceFacultyNameAr"",
        MAX(faculty_name_s) AS ""SourceFacultyNameEn""
    FROM major_info1_vw
    WHERE major_no IS NOT NULL
    GROUP BY major_no
    ORDER BY major_no";

                oracleMajors = oracleConnection
                    .Query<OracleMajorDto>(oracleQuery)
                    .ToList();
            }

            result.OracleCount = oracleMajors.Count;

            /*
             * ثانياً: المزامنة مع SQL Server.
             */
            using (var sqlConnection =
                   new SqlConnection(sqlConnectionString))
            {
                sqlConnection.Open();

                using (var transaction = sqlConnection.BeginTransaction())
                {
                    try
                    {
                        var existingMajorNumbers = sqlConnection
                            .Query<int>(
                                @"SELECT OracleMajorNo
                                  FROM AdmissionMajors
                                  WHERE OracleMajorNo IS NOT NULL",
                                transaction: transaction
                            )
                            .ToHashSet();

                        /*
                         * نعتبر التخصصات الرسمية غير موجودة مؤقتاً،
                         * وبعدها نعيد تفعيل كل تخصص وصل من Oracle.
                         */
                        sqlConnection.Execute(
                            @"UPDATE AdmissionMajors
                              SET ExistsInOracle = 0
                              WHERE OracleMajorNo IS NOT NULL
                                AND IsLocalOnly = 0",
                            transaction: transaction
                        );

                        string syncSql = @"
    UPDATE AdmissionMajors
    SET
        SourceMajorNameAr = @SourceMajorNameAr,
        SourceMajorNameEn = @SourceMajorNameEn,
        SourceDegreeCode = @SourceDegreeCode,
        SourceFacultyNo = @SourceFacultyNo,
        SourceFacultyNameAr = @SourceFacultyNameAr,
        SourceFacultyNameEn = @SourceFacultyNameEn,
        ExistsInOracle = 1,
        LastOracleSyncAt = SYSDATETIME()
    WHERE OracleMajorNo = @OracleMajorNo;

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO AdmissionMajors
        (
            OracleMajorNo,
            SourceMajorNameAr,
            SourceMajorNameEn,
            SourceDegreeCode,
            SourceFacultyNo,
            SourceFacultyNameAr,
            SourceFacultyNameEn,
            IsLocalOnly,
            IsEnabledForAdmission,
            ExistsInOracle,
            LastOracleSyncAt,
            CreatedBy
        )
        VALUES
        (
            @OracleMajorNo,
            @SourceMajorNameAr,
            @SourceMajorNameEn,
            @SourceDegreeCode,
            @SourceFacultyNo,
            @SourceFacultyNameAr,
            @SourceFacultyNameEn,
            0,
            1,
            1,
            SYSDATETIME(),
            N'Oracle Sync'
        );
    END;";

                        foreach (var major in oracleMajors)
                        {
                            bool alreadyExists =
                                existingMajorNumbers.Contains(
                                    major.OracleMajorNo
                                );

                            sqlConnection.Execute(
                                syncSql,
                                major,
                                transaction
                            );

                            if (alreadyExists)
                            {
                                result.UpdatedCount++;
                            }
                            else
                            {
                                result.AddedCount++;
                            }
                        }

                        result.MissingFromOracleCount =
                            sqlConnection.ExecuteScalar<int>(
                                @"SELECT COUNT(*)
                                  FROM AdmissionMajors
                                  WHERE OracleMajorNo IS NOT NULL
                                    AND IsLocalOnly = 0
                                    AND ExistsInOracle = 0",
                                transaction: transaction
                            );

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }

            return result;
        }
    }
}