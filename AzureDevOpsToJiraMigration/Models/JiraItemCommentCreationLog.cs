using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models
{
    public class JiraItemCommentCreationLog: JiraItemCreationLog
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool Comment { get; set; } = true;
    }
}
