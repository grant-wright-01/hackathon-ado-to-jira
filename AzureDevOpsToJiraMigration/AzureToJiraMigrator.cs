using AzureDevOpsToJiraMigration.DataMapping;

namespace AzureDevOpsToJiraMigration
{
    public class AzureToJiraMigrator : IAzureToJiraMigrator
    {
        private readonly IAzureDevOpsClientWrapper _azureClient;
        private readonly IJiraClientWrapper _jiraWrapper;
        private readonly IAzureToJiraPropertyMapper _azureToJiraPropertyMapper;

        public AzureToJiraMigrator(
            IAzureDevOpsClientWrapper azureClient, 
            IJiraClientWrapper jiraWrapper, 
            IAzureToJiraPropertyMapper azureToJiraPropertyMapper)
        {
            _azureClient = azureClient;
            _jiraWrapper = jiraWrapper;
            _azureToJiraPropertyMapper = azureToJiraPropertyMapper;
        }

        public async Task Migrate()
        {
            var azureItems = await _azureClient.GetWorkItems();
            var mappedJiraItems = await _azureToJiraPropertyMapper.MapAzureItemsToJiraItems(azureItems);
            await _jiraWrapper.CreateJiraItems(mappedJiraItems);
        }
    }
}
