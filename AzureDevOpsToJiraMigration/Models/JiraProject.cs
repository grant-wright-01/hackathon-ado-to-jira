
namespace AzureDevOpsToJiraMigration.Models
{
    public class JiraProject
    {
        public string Id { get; set; }
        public Dictionary<string, string> IssueTypes { get; set; }
    }
}
