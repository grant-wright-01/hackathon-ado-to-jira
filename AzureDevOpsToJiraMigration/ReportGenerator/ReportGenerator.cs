using AzureDevOpsToJiraMigration.Models;
using System.Diagnostics;
using System.Text;

namespace AzureDevOpsToJiraMigration.ReportGenerator
{
    public class ReportGenerator : IReportGenerator
    {
        public async Task GenerateReport(MigrationLog migrationLog)
        {
            var htmlTemplateText = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "ReportGenerator", "index-template.html"));

            var htmlContent = htmlTemplateText
                .Replace("{{@user}}", migrationLog.User)
                .Replace("{{@startTime}}", migrationLog.StartTime.ToString())
                .Replace("{{@endTime}}", migrationLog.EndTime.ToString())
                .Replace("{{@durationInSeconds}}", migrationLog.DurationInSeconds.ToString())
                .Replace("{{@numberOfTicketsToProcess}}", migrationLog.NumberOfTicketsToProcess.ToString())
                .Replace("{{@numberOfSuccessfulMigrations}}", migrationLog.NumberOfSuccessfulMigrations.ToString())
                .Replace("{{@numberOfFailedMigrations}}", migrationLog.NumberOfFailedMigrations.ToString())
                .Replace("{{@migrationResultsBody}}", GenerateMigrationResultsBody(migrationLog));

            await CreateHtmlFile(htmlContent);
        }

        private string GenerateMigrationResultsBody(MigrationLog migrationLog)
        {
            var builder = new StringBuilder();
            var counter = 0;
            foreach (var item in migrationLog.JiraItemCreationLogs) 
            {
                builder.Append($"<tr>\r\n\t\t<td><a href=\"{item.AzureItemUrl}\">{item.AzureTicketId}</td>\r\n\t\t" +
                    $"<td><a href=\"{item.JiraTicketUrl}\">{item.JiraTicketId}</td>" +
                    $"<td>{GenerateStatusIcon(item)}</td>" +
                    $"<td id=\"request-body-button-{counter}\"><a class=\"btn btn-primary\" onclick=\"toggleRequestVisibility({counter})\">Request Body</a></td>" +
                    $"<td style=\"display:none;\" id=\"request-body-text-{counter}\"><a class=\"btn btn-primary\" onclick=\"toggleRequestVisibility({counter})\">{item.RequestBody}</a></td>" +
                    $"<td id=\"response-body-button-{counter}\"><a class=\"btn btn-primary\" onclick=\"toggleResponseVisibility({counter})\">Response Body</a></td>" +
                    $"<td style=\"display:none;\" id=\"response-body-text-{counter}\"><a class=\"btn btn-primary\" onclick=\"toggleResponseVisibility({counter})\">{item.ResponseBody}</a></td>" +
                    $"\r\n\t  </tr>");
                counter++;
            }

            return builder.ToString();
        }

        private string GenerateStatusIcon(JiraItemCreationLog logItem)
        {
            return logItem.IsSuccess ? "<i class=\"bi bi-check\"></i>" : "<i class=\"bi bi-file-x-fill\"></i>";
        }

        private async Task CreateHtmlFile(string htmlContent)
        {
            var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MigrationLogs");

            if (!Path.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, $"{Guid.NewGuid()}.html");
            await File.WriteAllTextAsync(filePath, htmlContent);

            var fileToOpen = filePath;
            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = true,
                FileName = fileToOpen
            };

            process.Start();
        }
    }
}
