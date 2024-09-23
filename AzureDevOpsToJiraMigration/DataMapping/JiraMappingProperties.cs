using AzureDevOpsToJiraMigration.Models;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public class JiraMappingProperties
    {
        public IEnumerable<JiraItemIssueType> IssueTypes { get; set; }
        public string ProjectId { get; set; }
        public string UserId { get; set; }
    }
}
