using System.Text.Json.Serialization;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class JiraItem
    {
        [JsonIgnore]
        public string AzureTicketNumber { get; set; }

        [JsonIgnore]
        public string AzureParentTicketNumber { get; set; }

        public Fields Fields { get; set; }
        public Update Update { get; set; }
    }
}
