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
        //public Reporter Reporter { get; set; }
        public string Summary { get; set; }
        //[JsonPropertyName("customfield_10054")]
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public double? Customfield_10054 { get; set; } // Story Point
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public Team Customfield_10001 { get; set; } // Team
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public Sprint Customfield_10020 { get; set; } // sprint number
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public Status Status { get; set; }
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public JiraItemComment Comment { get; set; }
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        //public double? StoryPointEstimate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Parent Parent { get; set; }
    }
}
