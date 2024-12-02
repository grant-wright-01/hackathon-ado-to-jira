using Microsoft.TeamFoundation.Work.WebApi;
using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class Fields
    {
        [JsonIgnore]
        public string WorkItemType { get; set; }
        public Assignee Assignee { get; set; }
        public Description Description { get; set; }
        public IssueType Issuetype { get; set; }
        public IEnumerable<string> Labels { get; set; }
        public Project Project { get; set; }
        public Reporter Reporter { get; set; }
        public string Summary { get; set; }
        //[JsonPropertyName("customfield_10054")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public double? StoryPointEstimate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Parent Parent { get; set; }
    }
}
