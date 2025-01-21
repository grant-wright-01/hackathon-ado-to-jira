using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class CommentBody
    {
        [JsonPropertyName("content")]
        public IEnumerable<Content> Content { get; set; }
        public string Type { get; set; }
        [JsonPropertyName("version")]
        public int Version { get; set; }
    }
}
