using AzureDevOpsToJiraMigration.Models.JiraItem;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public interface IAzureToJiraItemMapper
    {
        public bool IsMatch(string workItemType);
        JiraItem? Create(WorkItem workItem, JiraMappingProperties jiraProperties);
    }
}
