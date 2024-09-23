namespace AzureDevOpsToJiraMigration.Models
{
    public class MigrationLog
    {
        public string User { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double DurationInSeconds { get; set; }
        public int NumberOfTicketsToProcess { get; set; }
        public int NumberOfSuccessfulMigrations { get; set; }
        public int NumberOfFailedMigrations { get; set; }
        public IEnumerable<JiraItemCreationLog> JiraItemCreationLogs { get; set; }
    }
}
