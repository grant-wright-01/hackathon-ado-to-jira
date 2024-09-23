using AzureDevOpsToJiraMigration.DataMapping.MappingTypes.Bug;
using AzureDevOpsToJiraMigration.DataMapping.MappingTypes.Task;
using AzureDevOpsToJiraMigration.Models.JiraItem;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping.Factories
{
    public class AzureToJiraDataMapperFactory : IAzureToJiraDataMapperFactory
    {
        private readonly IEnumerable<IAzureToJiraItemMapper> _mappers;

        public AzureToJiraDataMapperFactory(IEnumerable<IAzureToJiraItemMapper> mappers)
        {
            _mappers = mappers;
        }

        public JiraItem? Create(WorkItem workItem, JiraMappingProperties jiraMappingProperties)
        {
            var workItemType = workItem.GetValueAsString("System.WorkItemType");

            var mapper = _mappers.FirstOrDefault(x => x.IsMatch(workItemType));
            
            if (mapper == null)
            {
                return null;
            }
            
            return mapper.Create(workItem, jiraMappingProperties);
        }
    }
}
