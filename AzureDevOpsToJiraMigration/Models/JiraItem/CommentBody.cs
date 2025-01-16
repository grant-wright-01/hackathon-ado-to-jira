using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class CommentBody
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }
    }
}
