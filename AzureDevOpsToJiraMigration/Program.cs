using AzureDevOpsToJiraMigration;
using AzureDevOpsToJiraMigration.DataMapping;
using AzureDevOpsToJiraMigration.DataMapping.Factories;
using AzureDevOpsToJiraMigration.DataMapping.MappingTypes.Bug;
using AzureDevOpsToJiraMigration.DataMapping.MappingTypes.Task;
using AzureDevOpsToJiraMigration.Options;
using AzureDevOpsToJiraMigration.ReportGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

IConfiguration config = builder.Build();

var serviceCollection = new ServiceCollection();
ConfigureServices(serviceCollection, config);

var serviceProvider = serviceCollection.BuildServiceProvider();
var azureToJiraMigrator = serviceProvider.GetService<IAzureToJiraMigrator>()!;

await azureToJiraMigrator.Migrate();

static void ConfigureServices(ServiceCollection serviceCollection, IConfiguration configuration)
{
    serviceCollection.AddOptions<AzureOptions>().Bind(configuration.GetSection("AzureOptions"));
    serviceCollection.AddOptions<JiraOptions>().Bind(configuration.GetSection("JiraOptions"));
    serviceCollection.AddTransient<IAzureToJiraMigrator, AzureToJiraMigrator>();
    serviceCollection.AddTransient<IAzureDevOpsClientWrapper, AzureDevOpsClientWrapper>();
    serviceCollection.AddTransient<IAzureToJiraPropertyMapper, AzureToJiraPropertyMapper>();
    serviceCollection.AddTransient<IJiraClientWrapper, JiraClientWrapper>();
    serviceCollection.AddTransient<IAzureToJiraDataMapperFactory, AzureToJiraDataMapperFactory>();
    serviceCollection.AddTransient<IReportGenerator, ReportGenerator>();
    serviceCollection.AddTransient<IAzureToJiraItemMapper, AzureToJiraBugMapper>();
    serviceCollection.AddTransient<IAzureToJiraItemMapper, AzureToJiraTaskMapper>();
}