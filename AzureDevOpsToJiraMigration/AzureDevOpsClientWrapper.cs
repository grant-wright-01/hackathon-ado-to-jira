using AzureDevOpsToJiraMigration.Options;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOpsToJiraMigration
{
    public class AzureDevOpsClientWrapper : IAzureDevOpsClientWrapper, IDisposable
    {
        private VssConnection _devOpsConnection;
        private readonly IOptions<AzureOptions> _azureOptions;

        public AzureDevOpsClientWrapper(IOptions<AzureOptions> azureOptions)
        {
            _azureOptions = azureOptions;
            _devOpsConnection = new VssConnection(new Uri(_azureOptions.Value.OrgUrl), new VssBasicCredential(string.Empty, _azureOptions.Value.PersonalAccessToken));
        }

        public async Task<IEnumerable<WorkItem>> GetWorkItems()
        {
            var workItemIds = new List<WorkItem>();
            WorkItemTrackingHttpClient witClient = _devOpsConnection.GetClient<WorkItemTrackingHttpClient>();

            List<QueryHierarchyItem> queryHierarchyItems = await witClient.GetQueriesAsync(_azureOptions.Value.TeamProjectName, depth: 2);

            QueryHierarchyItem myQueriesFolder = queryHierarchyItems.FirstOrDefault(qhi => qhi.Name.Equals("My Queries"));

            if (myQueriesFolder == null)
            {
                throw new Exception("Unable to find 'My Queries' folder");
            }
            
            var getWorkItemsQuery = await GetWorkItemClient(witClient, myQueriesFolder);
            WorkItemQueryResult result = await witClient.QueryByIdAsync(getWorkItemsQuery.Id);

            if (!result.WorkItems.Any())
            {
                Console.WriteLine("No work items were returned from query.");
            }

            int skip = 0;
            const int batchSize = 100;
            IEnumerable<WorkItemReference> workItemRefs;
            do
            {
                workItemRefs = result.WorkItems.Skip(skip).Take(batchSize);
                if (workItemRefs.Any())
                {
                    var workItems = await witClient.GetWorkItemsAsync(workItemRefs.Select(wir => wir.Id));
                    workItemIds.AddRange(workItems);
                }
                skip += batchSize;
            }
            while (workItemRefs.Count() == batchSize);

            Console.WriteLine($"Found {workItemIds.Count} Work Items in {_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}");
            return workItemIds;
        }

        private async Task<QueryHierarchyItem> CreateQuery(WorkItemTrackingHttpClient witClient, QueryHierarchyItem myQueriesFolder, string queryName)
        {
            var getWorkItemsQuery = new QueryHierarchyItem()
            {
                Name = queryName,
                Wiql = "SELECT [System.Id],[System.WorkItemType],[System.Title],[System.AssignedTo],[System.State],[System.Tags] FROM WorkItems",
                IsFolder = false
            };
            getWorkItemsQuery = await witClient.CreateQueryAsync(getWorkItemsQuery, _azureOptions.Value.TeamProjectName, myQueriesFolder.Name);

            return getWorkItemsQuery;
        }

        private async Task<QueryHierarchyItem> GetWorkItemClient(WorkItemTrackingHttpClient witClient, QueryHierarchyItem myQueriesFolder)
        {
            string queryName = "Get Everything";
            QueryHierarchyItem getWorkItemsQuery = null;

            if (myQueriesFolder.Children != null)
            {
                getWorkItemsQuery = myQueriesFolder.Children.FirstOrDefault(qhi => qhi.Name.Equals(queryName));
            }

            if (getWorkItemsQuery == null)
            {
                getWorkItemsQuery = new QueryHierarchyItem()
                {
                    Name = queryName,
                    Wiql = "SELECT [System.Id],[System.WorkItemType],[System.Title],[System.AssignedTo],[System.State],[System.Tags] FROM WorkItems",
                    IsFolder = false
                };
                getWorkItemsQuery = await witClient.CreateQueryAsync(getWorkItemsQuery, _azureOptions.Value.TeamProjectName, myQueriesFolder.Name);
            }

            return getWorkItemsQuery;
        }

        public void Dispose()
        {
            _devOpsConnection.Dispose();
        }
    }
}