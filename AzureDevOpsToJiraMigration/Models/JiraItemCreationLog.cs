using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models
{
    public class JiraItemCreationLog
    {
        public required string AzureTicketId { get; set; }
        public required string AzureItemUrl { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? JiraTicketId { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? JiraTicketUrl { get; set; }
        public bool IsSuccess { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StatusCode { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RequestBody { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ResponseBody { get; set; }
    }
}
