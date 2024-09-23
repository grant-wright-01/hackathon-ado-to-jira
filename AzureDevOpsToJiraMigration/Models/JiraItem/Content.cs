using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class Content
    {
        [JsonPropertyName("content")]
        public IEnumerable<InnerContent> Contents { get; set; }
        public string Type { get; set; }
    }
}
