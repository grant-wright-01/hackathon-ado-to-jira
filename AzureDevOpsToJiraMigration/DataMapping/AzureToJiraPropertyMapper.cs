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
            var jiraMappingProperties = await GenerateJiraMappingProperties(azureItems);

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

        private async Task<Dictionary<string, string>> GetJiraUsersFromAzureUsers(IEnumerable<WorkItem> azureItems)
        {
            var uniqueAzureUsers = new List<string>();
            foreach (var azureItem in azureItems.Where(x => x.Fields.ContainsKey("System.AssignedTo")))
            {
                var identityReference = ((Microsoft.VisualStudio.Services.WebApi.IdentityRef)azureItem.Fields["System.AssignedTo"]);
                var emailAddress = identityReference.UniqueName;
                var isInactive = identityReference.Inactive;

                if (!isInactive && 
                    !uniqueAzureUsers.Contains(emailAddress) && 
                    !emailAddress.Contains("OIDCONFLICT", StringComparison.CurrentCultureIgnoreCase) && 
                    emailAddress.EndsWith("@sainsburys.co.uk", StringComparison.CurrentCultureIgnoreCase))
                {
                    uniqueAzureUsers.Add(emailAddress);
                }
            }

            uniqueAzureUsers.Sort();
            var jiraUsers = new Dictionary<string, string>();

            foreach (var azureUser in uniqueAzureUsers)
            {
                try
                {
                    var jiraUser = await _jiraClientWrapper.GetUserId(azureUser);
                    jiraUsers.Add(azureUser, jiraUser);
                }
                catch
                {
                    continue;
                }
            }

            return jiraUsers;
        }

        private async Task<JiraMappingProperties> GenerateJiraMappingProperties(IEnumerable<WorkItem> azureItems)
        {
            var productId = await _jiraClientWrapper.GetProjectId();
            var jiraIssueTypes = await _jiraClientWrapper.GetAllIssueTypes(productId);

            // default person for the tickets on the board
            var defaultUser = await _jiraClientWrapper.GetUserId("trevor.baker@sainsburys.co.uk");

            return new JiraMappingProperties
            {
                IssueTypes = jiraIssueTypes,
                ProjectId = productId,
                DefaultUserId = defaultUser,
                UserIdMapping = await GetJiraUsersFromAzureUsers(azureItems)
            };
        }
    }
}
