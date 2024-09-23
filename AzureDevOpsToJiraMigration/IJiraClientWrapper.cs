using AzureDevOpsToJiraMigration.Models;
using AzureDevOpsToJiraMigration.Models.JiraItem;

namespace AzureDevOpsToJiraMigration
{
    public interface IJiraClientWrapper
    {
        Task CreateJiraItems(IEnumerable<JiraItem> jiraItems);
        Task<IEnumerable<JiraItemIssueType>> GetAllIssueTypes();
        Task<string> GetProjectId();
        Task<string> GetUserId();
    }
}