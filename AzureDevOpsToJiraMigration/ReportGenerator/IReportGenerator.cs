using AzureDevOpsToJiraMigration.Models;

namespace AzureDevOpsToJiraMigration.ReportGenerator
{
    public interface IReportGenerator
    {
        Task GenerateReport(MigrationLog migrationLog);
    }
}
