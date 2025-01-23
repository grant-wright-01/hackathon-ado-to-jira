using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Text;

namespace AzureDevOpsToJiraMigration.DataMapping.MappingTypes.Task
{
    public class AzureToJiraTaskMapper : IAzureToJiraItemMapper
    {
        private readonly IOptions<AzureOptions> _azureOptions;
        private readonly IJiraClientWrapper jiraClientWrapper;

        public AzureToJiraTaskMapper(IOptions<AzureOptions> azureOptions)
        {
            _azureOptions = azureOptions;
        }

        public async Task<JiraItem?> Create(WorkItem workItem, JiraMappingProperties jiraProperties)
        {
            var workItemType = workItem.GetValueAsString("System.WorkItemType");

            if (workItemType.Equals("user story", StringComparison.CurrentCultureIgnoreCase))
            {
                workItemType = "Story";
            }

            var parentId = workItem.GetParentId();
            if (workItemType.Equals("task", StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrEmpty(parentId))
            {
                workItemType = "Sub-task";
            }

            if (workItemType.Equals("spike", StringComparison.CurrentCultureIgnoreCase) ||
                string.Equals(workItemType, "feature", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "question", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "issue", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "deployment", StringComparison.OrdinalIgnoreCase))
            {
                workItemType = "Task";
            }

            var matchingIssueType = jiraProperties.IssueTypes.FirstOrDefault(x => x.Name == workItemType);

            if (matchingIssueType == null)
            {
                return null;
            }

            var assigneeId = workItem.GetMatchingOrDefaultUserId(jiraProperties);

            var jiraItem = new JiraItem
            {
                AzureTicketNumber = workItem.Id.ToString()!,
                AzureParentTicketNumber = parentId,
                Fields = new Fields
                {
                    WorkItemType = workItemType,
                    Assignee = new Assignee
                    {
                        Id = assigneeId
                    },
                    Description = new Description
                    {
                        Contents = new List<Content>
                        {
                            new Content
                            {
                                Contents = new List<InnerContent>
                                {
                                    new InnerContent
                                    {
                                        Text = GenerateDescription(workItem),
                                        Type = "text"
                                    }
                                },
                                Type = "paragraph"
                            },
                        },
                        Type = "doc",
                        Version = 1
                    },
                    Issuetype = new IssueType
                    {
                        Id = matchingIssueType.Id
                    },
                    Labels = workItem.GetTags(),
                    Project = new Project
                    {
                        Id = jiraProperties.ProjectId
                    },
                    //Status = new Status
                    //{
                    //    Name = workItem.GetValueAsString("System.State")
                    //},
                    Customfield_10020 = new Sprint
                    {
                        Id = 28127,
                        Name = $"TOR {workItem.GetSprint()}",
                    }, // Sprint
                    //Customfield_10001 = new Team
                    //{
                    //    Id = "0e7b2cd5-58cc-4bdf-a03d-1056932bf9f8-190",
                    //    Name = "Tornado",
                    //    Title = "Tornado",
                    //}, // Team
                    //Customfield_10054 = workItem.GetValue<double?>("Microsoft.VSTS.Scheduling.StoryPoints"), // story point
                    //Reporter = new Reporter
                    //{
                    //    EmailAddress = ((Microsoft.VisualStudio.Services.WebApi.IdentityRef)workItem.Fields["System.AssignedTo"]).UniqueName
                    //},
                    Summary = GenerateSummary(workItem),
                    //Comment = await workItem.GetComments(_azureOptions.Value)
                    //StoryPointEstimate = workItem.GetValue<double?>("Microsoft.VSTS.Scheduling.StoryPoints")
                },
                Update = new Update()
            };

            return jiraItem;
        }

        public bool IsMatch(string workItemType)
        {
            return string.Equals(workItemType, "task", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "feature", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "epic", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "story", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "question", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "user story", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "issue", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "deployment", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(workItemType, "spike", StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateSummary(WorkItem workItem)
        {
            var workItemType = workItem.GetValueAsString("System.WorkItemType");
            var summary = string.Empty;

            if (workItemType.Equals("deployment", StringComparison.CurrentCultureIgnoreCase))
            {
                summary += "DEPLOYMENT: ";
            }

            if (workItemType.Equals("spike", StringComparison.CurrentCultureIgnoreCase))
            {
                summary += "SPIKE: ";
            }

            if (workItemType.Equals("epic", StringComparison.CurrentCultureIgnoreCase))
            {
                summary += "EPIC: ";
            }

            if (workItemType.Equals("question", StringComparison.CurrentCultureIgnoreCase))
            {
                summary += "QUESTION: ";
            }

            summary += workItem.GetValueAsString("System.Title");

            return summary;
        }

        private string GenerateDescription(WorkItem workItem)
        {
            var descriptionBuilder = new StringBuilder();
            var description = workItem.GetValueAsString("System.Description");
            var azureTicketUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{workItem.Id}";

            string sprint = workItem.GetSprint();
            if (!string.IsNullOrEmpty(sprint))
            {
                descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Issue sprint: {sprint}{Environment.NewLine}");
            }

            string status = workItem.GetValueAsString("System.State");
            if (!string.IsNullOrEmpty(status))
            {
                descriptionBuilder.AppendLine($"Issue Status: {status}{Environment.NewLine}");
            }

            descriptionBuilder.AppendLine($"Description:{Environment.NewLine}{description}");

            if (workItem.Fields.ContainsKey("Microsoft.VSTS.Common.AcceptanceCriteria"))
            {
                var acceptanceCriteria = workItem.Fields["Microsoft.VSTS.Common.AcceptanceCriteria"].ToString();
                descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Acceptance criteria:");
                descriptionBuilder.AppendLine(acceptanceCriteria);
            }

            var storyPoints = workItem.GetValueAsString("Microsoft.VSTS.Scheduling.StoryPoints");
            if (!string.IsNullOrEmpty(storyPoints))
            {
                descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Story points: {storyPoints}");
            } 

            descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Azure ticket url:");
            descriptionBuilder.AppendLine(azureTicketUrl);
            return descriptionBuilder.ToString().StripHTML().Trim();
        }
    }
}
