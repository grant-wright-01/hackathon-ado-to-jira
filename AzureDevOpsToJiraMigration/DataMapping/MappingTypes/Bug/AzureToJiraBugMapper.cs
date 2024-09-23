using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.Text;

namespace AzureDevOpsToJiraMigration.DataMapping.MappingTypes.Bug
{
    public class AzureToJiraBugMapper : IAzureToJiraItemMapper
    {
        private readonly IOptions<AzureOptions> _azureOptions;

        public AzureToJiraBugMapper(IOptions<AzureOptions> azureOptions)
        {
            _azureOptions = azureOptions;
        }

        public JiraItem Create(WorkItem workItem, JiraMappingProperties jiraProperties)
        {
            var matchingIssueType = jiraProperties.IssueTypes.First(x => x.Name == workItem.GetValueAsString("System.WorkItemType"));

            var jiraItem = new JiraItem
            {
                AzureTicketNumber = workItem.Id.ToString()!,
                Fields = new Fields
                {
                    Assignee = new Assignee
                    {
                        // note: this value can come from doing a lookup on the jira users api endpoint
                        Id = jiraProperties.UserId
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
                        Id = jiraProperties.UserId
                    },
                    Summary = workItem.GetValueAsString("System.Title")!,
                    StoryPointEstimate = workItem.GetValue<double?>("Microsoft.VSTS.Scheduling.StoryPoints")
                },
                Update = new Update()
            };

            return jiraItem;
        }

        public bool IsMatch(string workItemType)
        {
            return string.Equals(workItemType, "bug", StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateDescription(WorkItem workItem)
        {
            var descriptionBuilder = new StringBuilder();
            var reproSteps = $"{workItem.GetValueAsString("Microsoft.VSTS.TCM.ReproSteps")}{Environment.NewLine}";
            var systemInformation = $"{workItem.GetValueAsString("Microsoft.VSTS.TCM.SystemInfo")}";
            var azureTicketUrl = $"{_azureOptions.Value.OrgUrl}/{_azureOptions.Value.TeamProjectName}/_workitems/edit/{workItem.Id}";

            descriptionBuilder.AppendLine(reproSteps);
            descriptionBuilder.AppendLine(systemInformation);
            descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Azure ticket url:");
            descriptionBuilder.AppendLine(azureTicketUrl);
            return descriptionBuilder.ToString();
        }
    }
}
