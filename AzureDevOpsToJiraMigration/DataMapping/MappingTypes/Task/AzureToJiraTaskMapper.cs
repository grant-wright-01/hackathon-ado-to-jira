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

        public JiraItem? Create(WorkItem workItem, JiraMappingProperties jiraProperties)
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
                    Reporter = new Reporter
                    {
                        Id = assigneeId
                    },
                    Summary = workItem.GetValueAsString("System.Title")!,
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
                summary += "Deployment: ";
            }

            summary += workItem.GetValueAsString("System.Title");

            return summary;
        }

        private string GenerateDescription(WorkItem workItem)
        {
            var descriptionBuilder = new StringBuilder();
            var description = workItem.GetValueAsString("System.Description");
            var azureTicketUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{workItem.Id}";

            descriptionBuilder.AppendLine(description);

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
