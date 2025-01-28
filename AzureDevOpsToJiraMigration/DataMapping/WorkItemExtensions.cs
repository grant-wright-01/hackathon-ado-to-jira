using AzureDevOpsToJiraMigration.Models.JiraItem;
using AzureDevOpsToJiraMigration.Options;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Diagnostics;
using System.Text.RegularExpressions;

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

            if (workItem.GetValueAsString("System.IterationPath").Length != 0) 
            {
                tagList.Add( ReplaceWSpace(workItem.GetSprint(), "_") );
            }

            tagList.Add( ReplaceWSpace(workItem.GetValueAsString("System.State"),"_") );

            tagList.Add($"AzureItemId-{workItem.Id}");

            if (!workItem.Fields.ContainsKey("System.Tags"))
            {
                return tagList;
            }

            var csvTagValue = workItem.Fields["System.Tags"].ToString()!;

            tagList.AddRange(csvTagValue.Split(";").Select(x => x.Replace(" ", string.Empty).Trim()));

            return tagList;
        }

        private static readonly Regex wSpace = new Regex(@"\s+");
        public static string ReplaceWSpace(string text, string replacement)
        {
            return wSpace.Replace(text, replacement);
        }

        public static async Task<JiraItemComment> GetComments(this WorkItem workItem, AzureOptions azureOptions)
        {
            VssConnection devOpsConnection = new VssConnection(new Uri(azureOptions.OrgUrl), new VssBasicCredential(string.Empty, azureOptions.PersonalAccessToken));
            WorkItemTrackingHttpClient witClient = devOpsConnection.GetClient<WorkItemTrackingHttpClient>();

            var witComments = new CommentList();

            var azureCommentCount = workItem.GetValue<Int64>("System.CommentCount");

            if (azureCommentCount > 0 && workItem.Id != null) // if there are comments retrieve them
            {
                witComments = await witClient.GetCommentsAsync(azureOptions.TeamProjectName, (int)workItem.Id);

                var retrievedComments = new List<AzureDevOpsToJiraMigration.Models.JiraItem.Comment>();

                // map the necessary data from the fetched data
                foreach (var comment in witComments.Comments)
                {
                    AzureDevOpsToJiraMigration.Models.JiraItem.Comment mappedComment =
                                new AzureDevOpsToJiraMigration.Models.JiraItem.Comment
                                {
                                    Body = new CommentBody
                                    {
                                        Content = new List<Content>
                                        {
                                            new Content
                                            {
                                                Contents = new List<InnerContent>
                                                {
                                                    new InnerContent
                                                    {
                                                        Type = "text",
                                                        Text = $"Authored By: {comment.CreatedBy.UniqueName}{Environment.NewLine}{Environment.NewLine} {comment.Text.StripHTML().Trim()}" ,
                                                    }
                                                },
                                                Type = "paragraph",
                                            }
                                        },
                                        Type = "doc",
                                        Version = 1
                                    }
                                };
                    retrievedComments.Add(mappedComment);
                }

                // jira data to be returned
                var jiraMappedWitComments = new JiraItemComment
                {
                    Comments = retrievedComments,
                    Total = witComments.TotalCount
                };

                return jiraMappedWitComments;
            }

            return new JiraItemComment
            {
                Comments = new List<AzureDevOpsToJiraMigration.Models.JiraItem.Comment>(),
                Total = 0
            };
        }

        public static string GetSprint(this WorkItem workItem)
        {
            var iterationPath = workItem.GetValueAsString("System.IterationPath");

            string sprint = iterationPath.Split('\\').Last();

            return sprint;
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
