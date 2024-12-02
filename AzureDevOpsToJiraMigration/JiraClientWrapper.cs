using AzureDevOpsToJiraMigration.Models;
using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using AzureDevOpsToJiraMigration.ReportGenerator;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;

namespace AzureDevOpsToJiraMigration
{
    public class JiraClientWrapper : IJiraClientWrapper
    {
        private readonly IReportGenerator _reportGenerator;
        private readonly IOptions<JiraOptions> _jiraOptions;
        private readonly IOptions<AzureOptions> _azureOptions;
        private Dictionary<string, string> _azureIdToJiraId;
        private HttpClient _httpClient;

        public JiraClientWrapper(
            IReportGenerator reportGenerator,
            IOptions<JiraOptions> jiraOptions, 
            IOptions<AzureOptions> azureOptions)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(jiraOptions.Value.OrgUrl)
            };

            _azureIdToJiraId = new Dictionary<string, string>();
            _reportGenerator = reportGenerator;
            _jiraOptions = jiraOptions;
            _azureOptions = azureOptions;
        }

        public async Task<JiraProject> GetProjectData()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"rest/api/3/project/{_jiraOptions.Value.ProjectKey}");
            request.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

            var issueTypeResponse = await _httpClient.SendAsync(request);

            if (!issueTypeResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error attempting to get issue types from jira, status code: {issueTypeResponse.StatusCode}");
            }

            var content = await issueTypeResponse.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                throw new Exception("unable to get issue types from jira");
            }

            var jsonContent = JObject.Parse(content);
            var projectId = jsonContent["id"]!.ToString();

            var issueTypes = jsonContent["issueTypes"];

            var issueTypeMap = new Dictionary<string, string>();
            foreach (var item in issueTypes)
            {
                var id = item["id"].ToString();
                var type = item["name"].ToString();
                issueTypeMap.Add(type, id);
            }

            return new JiraProject 
            {
                Id = projectId,
                IssueTypes = issueTypeMap
            };
        }

        public async Task<IEnumerable<JiraItemIssueType>> GetAllIssueTypes(string productId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"rest/api/3/issuetype/project?projectId={productId}");
            request.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

            var issueTypeResponse = await _httpClient.SendAsync(request);

            if (!issueTypeResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error attempting to get issue types from jira, status code: {issueTypeResponse.StatusCode}");
            }

            var content = await issueTypeResponse.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content)) 
            {
                throw new Exception("unable to get issue types from jira");
            }

            var issueTypes = new List<JiraItemIssueType>();

            foreach (var child in JArray.Parse(content))
            {
                issueTypes.Add(new JiraItemIssueType
                {
                    Id = child["id"]!.ToString(),
                    Name = child["name"]!.ToString()
                });
            }

            return issueTypes;
        }

        public async Task<string> GetProjectId()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"rest/api/3/project/{_jiraOptions.Value.ProjectKey}");
            request.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

            var issueTypeResponse = await _httpClient.SendAsync(request);

            if (!issueTypeResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error attempting to get project from jira, status code: {issueTypeResponse.StatusCode}");
            }

            var content = await issueTypeResponse.Content.ReadAsStringAsync();
            return JObject.Parse(content)["id"]!.ToString();
        }

        public async Task<string> GetUserId(string emailAddress)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"rest/api/3/user/recommend?context=Reporter&project={_jiraOptions.Value.ProjectKey}" +
                $"&query={emailAddress}");

            request.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

            var issueTypeResponse = await _httpClient.SendAsync(request);

            if (!issueTypeResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error attempting to get user from jira, status code: {issueTypeResponse.StatusCode}");
            }

            var content = await issueTypeResponse.Content.ReadAsStringAsync();
            var users = JArray.Parse(content);

            var userMatchingOnEmail = users.FirstOrDefault(x => (x["emailAddress"]?.ToString() ?? "") == emailAddress);
            
            if (userMatchingOnEmail == null)
            {
                throw new Exception($"Error attempting to get user from jira, status code: {issueTypeResponse.StatusCode}");
            }
            
            return userMatchingOnEmail["accountId"]!.ToString();
        }

        private JsonSerializerOptions GetSerializerOptions()
        {
            return new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public async Task CreateHierachicalJiraItems(IEnumerable<IGrouping<string, JiraItem>> jiraItems)
        {
            var migrationLog = new MigrationLog
            {
                StartTime = DateTime.Now,
                User = _jiraOptions.Value.Username,
                NumberOfTicketsToProcess = jiraItems.Count()
            };

            var jiraLogMessages = new List<JiraItemCreationLog>();
            Console.WriteLine($"Attempting to create {jiraItems.Count()} jira items");
            var counter = 0;
            var successCounter = 0;
            var failedCounter = 0;
            var features = jiraItems.Where(x => x.Key == "Feature");

            foreach (var item in jiraItems.Where(x => x.Key != "Feature"))
            {
                foreach (var jiraItem in item)
                {
                    try
                    {

                        var parentId = jiraItem.AzureParentTicketNumber;
                        if (item.Key == "Story")
                        {
                            var epicId = features.FirstOrDefault(x => x.Any(y => y.AzureTicketNumber == jiraItem.AzureParentTicketNumber))?
                                .FirstOrDefault()?.AzureParentTicketNumber;

                            parentId = epicId;
                        }
                        
                        if (!string.IsNullOrEmpty(parentId))
                        {
                            if (_azureIdToJiraId.ContainsKey(parentId))
                            {
                                jiraItem.Fields.Parent = new Parent
                                {
                                    Key = _azureIdToJiraId[parentId]
                                };
                            }
                        }

                        counter++;
                        var jsonRequestString = JsonSerializer.Serialize(jiraItem, GetSerializerOptions());
                        var content = new StringContent(jsonRequestString, Encoding.UTF8, "application/json");
                        var request = new HttpRequestMessage(HttpMethod.Post, "rest/api/3/issue")
                        {
                            Content = content
                        };

                        request.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

                        var createJiraItemResponse = await _httpClient.SendAsync(request);
                        var responseContent = await createJiraItemResponse.Content.ReadAsStringAsync();

                        if (!createJiraItemResponse.IsSuccessStatusCode)
                        {
                            jiraLogMessages.Add(new JiraItemCreationLog
                            {
                                AzureTicketId = jiraItem.AzureTicketNumber,
                                AzureItemUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{jiraItem.AzureTicketNumber}",
                                IsSuccess = false,
                                RequestBody = jsonRequestString,
                                ResponseBody = responseContent,
                                StatusCode = (int)createJiraItemResponse.StatusCode
                            });

                            Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Failed to create jira item");
                            failedCounter++;
                            continue;
                        }

                        var responseObject = JObject.Parse(responseContent);
                        var createdItemId = responseObject["id"]!.ToString();
                        var createdItemKey = responseObject["key"]!.ToString();
                        Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Successfully created jira item: {createdItemId}");

                        jiraLogMessages.Add(new JiraItemCreationLog
                        {
                            AzureTicketId = jiraItem.AzureTicketNumber,
                            AzureItemUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{jiraItem.AzureTicketNumber}",
                            JiraTicketId = createdItemId,
                            JiraTicketUrl = $"{_jiraOptions.Value.OrgUrl}/jira/software/projects/{_jiraOptions.Value.ProjectKey}/list?selectedIssue={createdItemKey}",
                            IsSuccess = true,
                            RequestBody = jsonRequestString,
                            ResponseBody = responseContent
                        });


                        _azureIdToJiraId.Add(jiraItem.AzureTicketNumber, createdItemKey);
                        successCounter++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        failedCounter++;
                        continue;
                    }
                }
            }

            migrationLog.NumberOfFailedMigrations = failedCounter;
            migrationLog.NumberOfSuccessfulMigrations = successCounter;
            migrationLog.JiraItemCreationLogs = jiraLogMessages;
            migrationLog.EndTime = DateTime.Now;
            migrationLog.DurationInSeconds = (migrationLog.EndTime - migrationLog.StartTime).TotalSeconds;
            await _reportGenerator.GenerateReport(migrationLog);
        }
    }
}
