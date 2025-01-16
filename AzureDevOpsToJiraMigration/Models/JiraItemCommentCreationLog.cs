using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models
{
    public class JiraItemCommentCreationLog: JiraItemCreationLog
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Comment { get; set; }
    }
}
