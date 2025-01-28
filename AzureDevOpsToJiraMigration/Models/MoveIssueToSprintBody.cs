using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureDevOpsToJiraMigration.Models
{
    public class MoveIssueToSprintBody
    {
        [JsonPropertyName("issues")]
        public IEnumerable<string> Issues { get; set; }
    }
}
