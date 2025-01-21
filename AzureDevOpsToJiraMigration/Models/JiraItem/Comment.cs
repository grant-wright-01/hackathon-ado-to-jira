using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AzureDevOpsToJiraMigration.Models.JiraItem
{
    public class Comment
    {
        //public int Id { get; set; }
        //public CommentAuthor Author { get; set; }
        [JsonPropertyName("body")]
        public CommentBody Body { get; set; }    
        //public string Name { get; set; }
        //public string CommentText { get; set; }
        //public string CreatedBy { get; set; }
        //public DateTime CreatedDate { get; set; }
        //public bool IsDeleted { get; set; }
    }
}
