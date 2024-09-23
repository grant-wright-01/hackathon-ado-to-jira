using AzureDevOpsToJiraMigration.Models.JiraItem;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping.Factories
{
    public interface IAzureToJiraDataMapperFactory
    {
        JiraItem? Create(WorkItem workItem, JiraMappingProperties jiraMappingProperties);
    }
}
