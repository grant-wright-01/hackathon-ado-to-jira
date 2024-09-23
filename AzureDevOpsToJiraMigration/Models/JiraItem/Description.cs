using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class Description
    {
        [JsonPropertyName("content")]
        public IEnumerable<Content> Contents { get; set; }
        public string Type { get; set; }
        public int Version { get; set; }
    }
}
