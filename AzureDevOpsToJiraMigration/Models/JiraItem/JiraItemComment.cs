using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class JiraItemComment
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IEnumerable<Comment> Comments { get; set; }
        public int Total {  get; set; }
    }
}
