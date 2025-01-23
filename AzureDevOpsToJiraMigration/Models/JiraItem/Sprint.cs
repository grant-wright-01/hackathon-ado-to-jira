using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class Sprint
    {
        [JsonPropertyName("id")]
        public double Id { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Name { get; set; }
        public string State { get; set; } = "future";
        public int BoardId { get; set; } = 3950;
    }
}
