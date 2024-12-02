using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public static class WorkItemExtensions
    {
        public static string GetMatchingOrDefaultUserId(this WorkItem workItem, JiraMappingProperties jiraMappingProperties)
        {
            if (!workItem.Fields.ContainsKey("System.AssignedTo"))
            {
                return jiraMappingProperties.DefaultUserId;
            }

            var emailAddress = ((Microsoft.VisualStudio.Services.WebApi.IdentityRef)workItem.Fields["System.AssignedTo"]).UniqueName;
                
            return jiraMappingProperties.UserIdMapping.ContainsKey(emailAddress) ?
                jiraMappingProperties.UserIdMapping.First(x => x.Key.Equals(emailAddress, StringComparison.CurrentCultureIgnoreCase)).Value :
                jiraMappingProperties.DefaultUserId;
        }

        public static IEnumerable<string> GetTags(this WorkItem workItem)
        {
            var tagList = new List<string>();

            var commentCount = workItem.GetValue<Int64>("System.CommentCount");

            if (commentCount > 0)
            {
                tagList.Add("HasComments");
            }

            tagList.Add($"AzureItemId-{workItem.Id}");

            if (!workItem.Fields.ContainsKey("System.Tags"))
            {
                return tagList;
            }

            var csvTagValue = workItem.Fields["System.Tags"].ToString()!;

            tagList.AddRange(csvTagValue.Split(";").Select(x => x.Replace(" ", string.Empty).Trim()));

            return tagList;
        }

        public static string GetValueAsString(this WorkItem workItem, string key)
        {
            if (!workItem.Fields.ContainsKey(key))
            {
                return string.Empty;
            }

            return workItem.Fields[key].ToString()!;
        }

        public static string GetParentId(this WorkItem workItem)
        {
            if (workItem.Relations == null || !workItem.Relations.Any())
            {
                return string.Empty;
            }

            var parentId = workItem.Relations.FirstOrDefault(x => x.Attributes.ContainsKey("name") && 
                x.Attributes["name"].ToString()!.Equals("parent", StringComparison.OrdinalIgnoreCase))?.Url.Split('/').Last();

            return parentId ?? string.Empty;
        }

        public static T? GetValue<T>(this WorkItem workItem, string key)
        {
            if (!workItem.Fields.ContainsKey(key))
            {
                return default;
            }

            return (T)workItem.Fields[key];
        }
    }
}
