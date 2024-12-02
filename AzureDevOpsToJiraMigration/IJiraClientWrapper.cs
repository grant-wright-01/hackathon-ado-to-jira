using AzureDevOpsToJiraMigration.Models;
using AzureDevOpsToJiraMigration.Models.JiraItem;

namespace AzureDevOpsToJiraMigration
{
    public interface IJiraClientWrapper
    {
        Task<JiraProject> GetProjectData();
        Task<IEnumerable<JiraItemIssueType>> GetAllIssueTypes(string productId);
        Task<string> GetProjectId();
        Task<string> GetUserId(string emailAddress);
        Task CreateHierachicalJiraItems(IEnumerable<IGrouping<string, JiraItem>> items);
    }
}