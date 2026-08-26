using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using University_Agent_System.Models.Oracle;
using University_Agent_System.Models.ViewModel;

namespace University_Agent_System.Services
{
    public class StudentsBySemester
    {
        private readonly IDbConnection _db;
        private readonly IDbConnection _oracleDb;
        private readonly IConfiguration _configuration;
        public StudentsBySemester(IDbConnection db, IDbConnection oracleDb, IConfiguration configuration)
        {
            _db = db;
            string oracleConnStr = configuration.GetConnectionString("OracleConnection");
            _oracleDb = new OracleConnection(oracleConnStr);
            _configuration = configuration;
        }

        public List<StudentWithMajorVM> GetStudentsBySemester(string academicYear, int semester, int? agentId = null, string language = "en")
        {
            // 1. Build the semester ID from academic year and semester number
            string startYear = academicYear.Split('-')[0];
            string semesterId = $"{startYear}{semester}";

            // 2. Fetch majors for mapping
            var majors = _oracleDb.Query<ProgramVM>("SELECT major_no, Major_Name_S,MAJOR_NAME FROM major_info1_vw").ToList();

            // 3. Build SQL query and parameters
            string sql = "SELECT * FROM Students WHERE semesterId = @semesterId and active=1";
            var parameters = new DynamicParameters();
            parameters.Add("semesterId", semesterId);

            // 4. Apply agentId filter if available
            if (agentId.HasValue)
            {
                sql += " AND agentId = @agentId";
                parameters.Add("agentId", agentId.Value);
            }

            // 5. Execute query
            var students = _db.Query<StudentWithMajorVM>(sql, parameters).ToList();

            // 6. Map major name
            foreach (var s in students)
            {
                var matchedMajor = majors.FirstOrDefault(m => m.major_no == s.major_no);
                s.Major_Name_S = language == "ar" ? matchedMajor?.MAJOR_NAME ?? "N/A" : matchedMajor?.Major_Name_S ?? "N/A";

                //s.Major_Name_S = majors.FirstOrDefault(m => m.major_no == s.major_no)?.Major_Name_S ?? "N/A";
            }

            return students;
        }

    }
}
