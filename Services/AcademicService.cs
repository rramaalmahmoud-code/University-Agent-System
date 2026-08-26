using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using Microsoft.Extensions.Configuration;
namespace University_Agent_System.Services
{
    public class AcademicService
    {
        private readonly IDbConnection _db;
        private readonly IDbConnection _oracleDb;

        private readonly IConfiguration _configuration;
        public AcademicService(IDbConnection db, IDbConnection oracleDb, IConfiguration configuration)
        {
            _configuration = configuration;
            _db = db;
            string oracleConnStr = configuration.GetConnectionString("OracleConnection");
            _oracleDb = new OracleConnection(oracleConnStr);
        }

        //public List<SelectListItem> GetAcademicYears()
        //{
        //    var semesterList = _oracleDb.Query<int>("SELECT DISTINCT semester FROM TIME_TABLE_VW ORDER BY semester DESC").ToList();
        //    var academicYearList = semesterList
        //        .Select(semester => semester.ToString())
        //        .GroupBy(semester => semester.Substring(0, 4))
        //        .Select(group => new
        //        {
        //            StartYear = group.Key,
        //            EndYear = (int.Parse(group.Key) + 1).ToString(),
        //            AcademicYear = $"{group.Key}-{(int.Parse(group.Key) + 1)}"
        //        })
        //        .OrderByDescending(year => year.StartYear)
        //        .ToList();

        //    return academicYearList.Select(year => new SelectListItem
        //    {
        //        Value = year.AcademicYear,
        //        Text = year.AcademicYear
        //    }).ToList();
        //}
        public List<SelectListItem> GetAcademicYears()
        {
            var semesterList = _oracleDb
                .Query<int>(
                    @"SELECT DISTINCT semester
              FROM TIME_TABLE_VW
              WHERE semester IS NOT NULL
              ORDER BY semester DESC")
                .ToList();

            var startYears = semesterList
                .Select(semester => semester.ToString())
                .Where(semester => semester.Length >= 4)
                .Select(semester => int.Parse(semester.Substring(0, 4)))
                .Distinct()
                .ToList();

            // Add the next academic year after the latest existing year.
            // Example: latest is 2025-2026, so add 2026-2027.
            if (startYears.Any())
            {
                int latestStartYear = startYears.Max();
                int nextStartYear = latestStartYear + 1;

                if (!startYears.Contains(nextStartYear))
                {
                    startYears.Add(nextStartYear);
                }
            }

            return startYears
                .OrderByDescending(startYear => startYear)
                .Select(startYear => new SelectListItem
                {
                    Value = $"{startYear}-{startYear + 1}",
                    Text = $"{startYear}-{startYear + 1}"
                })
                .ToList();
        }

    }

}
