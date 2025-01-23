using AzureDevOpsToJiraMigration.DataMapping;
using AzureDevOpsToJiraMigration.Models;
using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using AzureDevOpsToJiraMigration.ReportGenerator;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AzureDevOpsToJiraMigration
{
    public class AzureToJiraMigrator : IAzureToJiraMigrator
    {
        private readonly IAzureDevOpsClientWrapper _azureClient;
        private readonly IJiraClientWrapper _jiraWrapper;
        private readonly IAzureToJiraPropertyMapper _azureToJiraPropertyMapper;
        private readonly IOptions<JiraOptions> _jiraOptions;
        private HttpClient _httpClient;

        public AzureToJiraMigrator(
            IAzureDevOpsClientWrapper azureClient, 
            IJiraClientWrapper jiraWrapper, 
            IAzureToJiraPropertyMapper azureToJiraPropertyMapper,
            IOptions<JiraOptions> jiraOptions)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(jiraOptions.Value.OrgUrl)
            };

            _jiraOptions = jiraOptions;
            _azureClient = azureClient;
            _jiraWrapper = jiraWrapper;
            _azureToJiraPropertyMapper = azureToJiraPropertyMapper;
        }

        public async Task Migrate()
        {
            var startDate = new DateTime(2023,11,15,0,0,0);
            int sprintTimelineInWeeks = 2;
            int firstSprintNumber = 45;
            int currentActiveSprint = 75;

            // OPTIONAL: automated creation of sprints in JIRA
            // i.e. run this alone by commenting the lines following this function call
            // when you intend to run this and comment this piece after sprints are created and uncomment the rest to continue with the migration
            await CreateSprints(startDate,sprintTimelineInWeeks,firstSprintNumber,currentActiveSprint); 

            //var azureItems = await _azureClient.GetWorkItems();

            //var latest = azureItems.OrderByDescending(x => x.Id).Take(50);

            //var mappedJiraItems = await _azureToJiraPropertyMapper.MapAzureItemsToJiraItems(latest);

            //var items = GroupAndOrderListByParent(mappedJiraItems);
            //await _jiraWrapper.CreateHierachicalJiraItems(items, latest);  // post jira items to jira board
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

        // creates sprints in JIRA board
        private async Task CreateSprints(DateTime startDate, int sprintTimelineInWeeks, int firstSprintNumber, int currentActiveSprint) // slight automation to the sprint creation within jira
        {
            var sprintNumber = firstSprintNumber;
            var daysInSprint = sprintTimelineInWeeks * 7 - 1; // where sprintTimelineInWeeks is the number of weeks
            var endDate = startDate.AddDays(daysInSprint);

            var state = ""; // future

            // if sprintNum < currentActiveSprint state = closed
            // if sprintNum == currentActiveSprint state = active
            // if sprintNum > currentActiveSprint state = future

            var sprintToCreate = new SprintCreationBody()
            {
                Name = $"TOR Sprint {sprintNumber}",
                StartDate = startDate,
                EndDate = endDate,
                //OriginBoardId = 3950, // jiraOptions boardId
            };

            var counter = 0;
            var successCounter = 0;
            var failedCounter = 0;

            do
            {
                counter++;

                var jsonRequestString = JsonSerializer.Serialize(sprintToCreate, GetSerializerOptions());
                var content = new StringContent(jsonRequestString, Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "rest/agile/1.0/sprint")
                {
                    Content = content
                };

                request.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

                var createJiraItemResponse = await _httpClient.SendAsync(request);
                var responseContent = await createJiraItemResponse.Content.ReadAsStringAsync();

                if (!createJiraItemResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Failed to create sprint");
                    failedCounter++;
                    continue;
                }

                successCounter++;
                sprintNumber++;
                Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Successfully created sprint: TOR Sprint {sprintNumber-1}");

                startDate = endDate.AddDays(1);
                endDate = startDate.AddDays(daysInSprint);

                sprintToCreate = new SprintCreationBody()
                {
                    Name = $"TOR Sprint {sprintNumber}",
                    StartDate = startDate,
                    EndDate = endDate,
                    //OriginBoardId = 3950, // jiraOptions boardId
                };
            }
            while(sprintNumber <= currentActiveSprint);
        }

        private JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }
    }
}
