using System.Text.RegularExpressions;

namespace AzureDevOpsToJiraMigration.DataMapping
{
    public static class StringExtensions
    {
        public static string StripHTML(this string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}
