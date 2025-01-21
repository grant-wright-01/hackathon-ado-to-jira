using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models.Process;
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

        public async Task<JiraItem> Create(WorkItem workItem, JiraMappingProperties jiraProperties)
        {
            var workItemType = workItem.GetValueAsString("System.WorkItemType");
            var matchingIssueType = jiraProperties.IssueTypes.First(x => x.Name == workItemType);
            var assigneeId = workItem.GetMatchingOrDefaultUserId(jiraProperties);

            var jiraItem = new JiraItem
            {
                AzureTicketNumber = workItem.Id.ToString()!,
                AzureParentTicketNumber = workItem.GetParentId(),
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
                    //////Customfield_10020 = new Sprint
                    //////{
                    //////    Id = 28127,
                    //////    Name = $"TOR {workItem.GetSprint()}",
                    //////}, // Sprint
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
                    Summary = workItem.GetValueAsString("System.Title")!,
                    //Comment = await workItem.GetComments(_azureOptions.Value)
                    //StoryPointEstimate = workItem.GetValue<double?>("Microsoft.VSTS.Scheduling.StoryPoints")
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

            string sprint = workItem.GetSprint();
            if (!string.IsNullOrEmpty(sprint))
            {
                descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Story sprint: {sprint}{Environment.NewLine}");
            }

            string status = workItem.GetValueAsString("System.State");
            if (!string.IsNullOrEmpty(status))
            {
                descriptionBuilder.AppendLine($"Story Status: {status}{Environment.NewLine}");
            }


            descriptionBuilder.AppendLine(reproSteps);
            descriptionBuilder.AppendLine(systemInformation);
            descriptionBuilder.AppendLine($"{Environment.NewLine}{Environment.NewLine}Azure ticket url:");
            descriptionBuilder.AppendLine(azureTicketUrl);
            return descriptionBuilder.ToString().StripHTML().Trim();
        }
    }
}
