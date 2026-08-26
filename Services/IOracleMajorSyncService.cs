using University_Agent_System.Models.Admission;

namespace University_Agent_System.Services
{
    public interface IOracleMajorSyncService
    {
        MajorSyncResult SyncMajors();
    }
}