namespace AzureDevOpsToJiraMigration.Options
{
    public class AzureOptions
    {
        public required string OrgUrl { get; set; }
        public required string TeamProjectName { get; set; }
        public required string PersonalAccessToken { get; set; }
    }
}
