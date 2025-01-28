using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class IssuesList
    {
        [JsonPropertyName("issues")]
        public IEnumerable<string> Issues { get; set; }
    }
}
