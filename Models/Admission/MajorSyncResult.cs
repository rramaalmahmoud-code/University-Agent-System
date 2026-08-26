namespace University_Agent_System.Models.Admission
{
    public class MajorSyncResult
    {
        public int OracleCount { get; set; }

        public int AddedCount { get; set; }

        public int UpdatedCount { get; set; }

        public int MissingFromOracleCount { get; set; }
    }
}