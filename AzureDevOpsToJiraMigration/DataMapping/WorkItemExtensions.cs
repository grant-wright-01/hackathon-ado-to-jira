using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public static class WorkItemExtensions
    {
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
