using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration
{
    public interface IAzureDevOpsClientWrapper
    {
        Task<IEnumerable<WorkItem>> GetWorkItems();
    }
}