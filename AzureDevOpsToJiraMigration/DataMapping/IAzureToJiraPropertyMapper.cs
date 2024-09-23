using AzureDevOpsToJiraMigration.Models.JiraItem;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public interface IAzureToJiraPropertyMapper
    {
        Task<IEnumerable<JiraItem>> MapAzureItemsToJiraItems(IEnumerable<WorkItem> azureItems);
    }
}