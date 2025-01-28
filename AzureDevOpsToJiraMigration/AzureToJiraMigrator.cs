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
            // values below can be transferred to appsettings
            var startDate = new DateTime(2023,11,15,0,0,0);
            int sprintTimelineInWeeks = 2;
            int firstSprintNumber = 45; // 45;
            int currentActiveSprint = 75; // 75;
            int latestCreatedSprint = 85; // 75; // get max sprint

            // OPTIONAL: automated creation of sprints in JIRA
            // i.e. you can either run this alone by commenting the lines following this function call
            // when you intend to run this and comment this piece after sprints are created and uncomment the rest to continue with the migration
            // or run it and get the dictionary of sprints created which will be later used to allocate issues to sprint in a latter function
            var createdJiraSprints = await CreateSprints(startDate,sprintTimelineInWeeks,firstSprintNumber,latestCreatedSprint);

            var azureItems = await _azureClient.GetWorkItems();

            var latest = azureItems.OrderByDescending(x => x.Id);//.Take(50);

            var mappedJiraItems = await _azureToJiraPropertyMapper.MapAzureItemsToJiraItems(latest);

            var items = GroupAndOrderListByParent(mappedJiraItems);
            await _jiraWrapper.CreateHierachicalJiraItems(items, latest, createdJiraSprints);  // post jira items to jira board
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
        private async Task<Dictionary<string, int>> CreateSprints(DateTime startDate, int sprintTimelineInWeeks, int firstSprintNumber, int latestCreatedSprint) // slight automation to the sprint creation within jira
        {
            var jiraSprintsDictionary = new Dictionary<string, int>();

            var sprintNumber = firstSprintNumber;
            var daysInSprint = sprintTimelineInWeeks * 7 - 1; // where sprintTimelineInWeeks is the number of weeks in a sprint = 13 days
            var endDate = startDate.AddDays(daysInSprint);

            // var state = ""; // future

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

                var responseObject = JObject.Parse(responseContent);
                var createdSprintId = responseObject["id"]!.ToString();

                jiraSprintsDictionary.Add(sprintToCreate.Name, Int32.Parse(createdSprintId)); // map sprints created in jira to their ids in dictionary

                successCounter++;
                sprintNumber++;
                Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Successfully created sprint: TOR Sprint {sprintNumber-1}");

                // initialise new sprint dates:
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
            while(sprintNumber <= latestCreatedSprint);

            return jiraSprintsDictionary;
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
