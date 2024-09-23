using AzureDevOpsToJiraMigration.DataMapping.Factories;
using AzureDevOpsToJiraMigration.Models.JiraItem;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public class AzureToJiraPropertyMapper : IAzureToJiraPropertyMapper
    {
        private readonly IAzureToJiraDataMapperFactory _azureDataMapperFactory;
        private readonly IJiraClientWrapper _jiraClientWrapper;

        public AzureToJiraPropertyMapper(IAzureToJiraDataMapperFactory azureDataMapperFactory, IJiraClientWrapper jiraClientWrapper)
        {
            _azureDataMapperFactory = azureDataMapperFactory;
            _jiraClientWrapper = jiraClientWrapper;
        }

        public async Task<IEnumerable<JiraItem>> MapAzureItemsToJiraItems(IEnumerable<WorkItem> azureItems)
        {
            var jiraMappingProperties = await GenerateJiraMappingProperties();

            var mappedJiraItems = new List<JiraItem>();

            foreach (var azureItem in azureItems)
            {
                var jiraItem = _azureDataMapperFactory.Create(azureItem, jiraMappingProperties);

                if (jiraItem != null)
                {
                    mappedJiraItems.Add(jiraItem);
                }
            }

            return mappedJiraItems;
        }

        private async Task<JiraMappingProperties> GenerateJiraMappingProperties()
        {
            var jiraIssueTypes = await _jiraClientWrapper.GetAllIssueTypes();
            var productId = await _jiraClientWrapper.GetProjectId();
            var userId = await _jiraClientWrapper.GetUserId();

            return new JiraMappingProperties
            {
                IssueTypes = jiraIssueTypes,
                ProjectId = productId,
                UserId = userId
            };
        }
    }
}
