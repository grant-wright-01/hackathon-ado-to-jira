namespace AzureDevOpsToJiraMigration.Options
{
    public class JiraOptions
    {
        public required string OrgUrl { get; set; }
        public required string Username { get; set; }
        public required string ApiToken { get; set; }
        public required string ProjectKey { get; set; }
    }
}
