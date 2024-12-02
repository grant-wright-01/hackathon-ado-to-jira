using AzureDevOpsToJiraMigration.DataMapping;
using AzureDevOpsToJiraMigration.Models.JiraItem;

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

            var latest = azureItems.OrderByDescending(x => x.Id).Take(50);

            var mappedJiraItems = await _azureToJiraPropertyMapper.MapAzureItemsToJiraItems(latest);

            var items = GroupAndOrderListByParent(mappedJiraItems);
            await _jiraWrapper.CreateHierachicalJiraItems(items);
        }

        private IEnumerable<IGrouping<string, JiraItem>> GroupAndOrderListByParent(IEnumerable<JiraItem> jiraItems)
        {
            var hierarchyOrder = new Dictionary<string, int>
            {
                { "Epic", 1 },
                { "Feature", 2 },
                { "Story", 3 },
                { "Spike", 4 },
                { "Bug", 5 },
                { "Task", 6 },
                { "Sub-task", 7 },
                { "Issue", 8 },
            };

            return jiraItems.GroupBy(x => x.Fields.WorkItemType).OrderBy(x => hierarchyOrder[x.Key]);
        }
    }
}
