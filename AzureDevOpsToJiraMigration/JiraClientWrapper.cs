using AzureDevOpsToJiraMigration.DataMapping;
using AzureDevOpsToJiraMigration.Models;
using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using AzureDevOpsToJiraMigration.ReportGenerator;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
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
            var testMatching = users[0]["emailAddress"];

            var userMatchingOnEmail = users.First();

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

        public async Task CreateHierachicalJiraItems(IEnumerable<IGrouping<string, JiraItem>> jiraItems, IEnumerable<WorkItem> azItems, Dictionary<string, int> sprintsDictionary)
        {
            var migrationLog = new MigrationLog
            {
                StartTime = DateTime.Now,
                User = _jiraOptions.Value.Username,
                NumberOfTicketsToProcess = (int)jiraItems.LongCount()
            };

            var jiraLogMessages = new List<JiraItemCreationLog>();
            Console.WriteLine($"Attempting to create {jiraItems.Count()} jira items");
            var counter = 0;
            var commentsCounter = 0;
            var successCounter = 0;
            var commentsSuccessCounter = 0;
            var failedCounter = 0;
            var failedCommentCounter = 0;
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

                        var azIndex = Int32.Parse(jiraItem.AzureTicketNumber);

                        // migrate comments
                        if (jiraItem.Fields.Labels.Contains("HasComments"))
                        {
                            
                            var wItemComments = await azItems.First(az => az.Id.Equals(azIndex)).GetComments(_azureOptions.Value);

                            foreach (var comment in wItemComments.Comments)
                            {
                                commentsCounter++;
                                var jsonCommentRequestString = JsonSerializer.Serialize(comment, GetSerializerOptions());
                                var commentContent = new StringContent(jsonCommentRequestString, Encoding.UTF8, "application/json");
                                var commentRequest = new HttpRequestMessage(HttpMethod.Post, $"rest/api/3/issue/{createdItemKey}/comment")
                                {

                                    Content = commentContent // new StringContent("{body: " + $"{commentContent}", Encoding.UTF8, "application/json")
                                };

                                commentRequest.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);
                                //commentRequest

                                var createJiraItemCommentResponse = await _httpClient.SendAsync(commentRequest);
                                var commentResponseContent = await createJiraItemCommentResponse.Content.ReadAsStringAsync();

                                if (!createJiraItemCommentResponse.IsSuccessStatusCode)
                                {
                                    jiraLogMessages.Add(new JiraItemCommentCreationLog
                                    {
                                        AzureTicketId = jiraItem.AzureTicketNumber,
                                        AzureItemUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{jiraItem.AzureTicketNumber}",
                                        IsSuccess = false,
                                        RequestBody = jsonCommentRequestString,
                                        ResponseBody = commentResponseContent,
                                        StatusCode = (int)createJiraItemResponse.StatusCode
                                    });

                                    Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({commentsCounter}) - Failed to create jira item comment");
                                    failedCommentCounter++;
                                    continue;
                                }
                                var commentResponseObject = JObject.Parse(commentResponseContent);
                                var createdCommentId = commentResponseObject["id"]!.ToString();

                                Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({commentsCounter}) - Successfully created jira item comment: {createdCommentId}");
                                commentsSuccessCounter++;
                            }
                            
                        }

                        var jiraSprint = azItems.First(az => az.Id.Equals(azIndex)).GetSprint(); // get issue sprint

                        if (jiraSprint != "Team Tornado")
                        {
                            if (jiraItem.Fields.Issuetype.Id != "10022")
                            {
                                var sprintId = sprintsDictionary.First(sprint => sprint.Key == $"TOR {jiraSprint}").Value; // get id of retrieved jira sprint against sprint dictionary
                                var sprintIssue = new MoveIssueToSprintBody()
                                {
                                    Issues = new List<string>() { createdItemId },
                                };

                                // sprint allocation
                                var jsonSprintAllocationRequestString = JsonSerializer.Serialize(sprintIssue, GetSerializerOptions());
                                var sprintAllocationContent = new StringContent(jsonSprintAllocationRequestString, Encoding.UTF8, "application/json");
                                var sprintAllocationRequest = new HttpRequestMessage(HttpMethod.Post, $"/rest/agile/1.0/sprint/{sprintId}/issue")
                                {
                                    Content = sprintAllocationContent
                                };

                                sprintAllocationRequest.Headers.Authorization = new BasicAuthenticationHeaderValue(_jiraOptions.Value.Username, _jiraOptions.Value.ApiToken);

                                var allocateJiraSprintResponse = await _httpClient.SendAsync(sprintAllocationRequest);
                                var sprintAllocationResponseContent = await allocateJiraSprintResponse.Content.ReadAsStringAsync();

                                if (!allocateJiraSprintResponse.IsSuccessStatusCode)
                                {
                                    jiraLogMessages.Add(new JiraItemCreationLog
                                    {
                                        AzureTicketId = jiraItem.AzureTicketNumber,
                                        AzureItemUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{jiraItem.AzureTicketNumber}",
                                        IsSuccess = false,
                                        RequestBody = jsonSprintAllocationRequestString,
                                        ResponseBody = sprintAllocationResponseContent,
                                        StatusCode = (int)allocateJiraSprintResponse.StatusCode
                                    });

                                    Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Failed to allocate jira issue to sprint.");
                                    continue;
                                }
                                Console.WriteLine($"{DateTime.Now.ToShortDateString() + " - " + DateTime.Now.ToLongTimeString()} - ({counter}) - Successfully allocated jira issue, {createdItemId}, to sprint: TOR {jiraSprint}.");
                            }
                        }
                        
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
            migrationLog.NumberOfFailedCommentsMigrations = failedCommentCounter;
            migrationLog.NumberOfSuccessfulMigrations = successCounter;
            migrationLog.NumberOfSuccessfulCommentsMigrations = commentsSuccessCounter;
            migrationLog.JiraItemCreationLogs = jiraLogMessages;
            migrationLog.EndTime = DateTime.Now;
            migrationLog.DurationInSeconds = (migrationLog.EndTime - migrationLog.StartTime).TotalSeconds;
            await _reportGenerator.GenerateReport(migrationLog);
        }
    }
}
