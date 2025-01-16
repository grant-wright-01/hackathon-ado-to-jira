using AzureDevOpsToJiraMigration.Models.JiraItem;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping.Factories
{
    public interface IAzureToJiraDataMapperFactory
    {
        Task<JiraItem?> Create(WorkItem workItem, JiraMappingProperties jiraMappingProperties);
    }
}
