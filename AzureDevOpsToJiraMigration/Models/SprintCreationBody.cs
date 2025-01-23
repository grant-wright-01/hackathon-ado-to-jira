using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureDevOpsToJiraMigration.Models
{
    public class SprintCreationBody
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }    //"TOR Sprint 75",
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; } //"2025-01-22T00:00:00.000Z",
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; } //"2025-02-05T00:00:00.000Z",
        //[JsonPropertyName("createdDate")]
        //public DateTime CreatedDate { get; set; } // current date,
        [JsonPropertyName("originBoardId")]
        public int OriginBoardId { get; set; } = 3950;
        [JsonPropertyName("goal")]
        public string Goal { get; set; } // ""
    }
}
