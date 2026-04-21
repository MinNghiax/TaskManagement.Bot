using Mezon.Sdk;
using Mezon.Sdk.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.Commands.Complain;
using TaskManagement.Bot.Application.Commands.Report;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Application.Commands.TeamCommands;
using TaskManagement.Bot.Application.Services;
using TaskManagement.Bot.Application.Services.Reminders;
using TaskManagement.Bot.Domain.Interfaces;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Repositories;

var configuration = TaskManagementDbContextConfiguration.BuildConfiguration();
var hostBuilder = Host.CreateApplicationBuilder(args);
var services = hostBuilder.Services;
Console.OutputEncoding = System.Text.Encoding.UTF8;
services.AddSingleton<MezonClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    var options = new MezonClientOptions
    {
        BotId = config["Mezon:BotId"]!,
        Token = config["Mezon:Token"]!,
        Host = config["Mezon:Host"] ?? "gw.mezon.ai",
        Port = config["Mezon:Port"] ?? "443",
        UseSSL = bool.Parse(config["Mezon:UseSsl"] ?? "true"),
        TimeoutMs = int.Parse(config["Mezon:TimeoutMs"] ?? "10000")
    };

    return new MezonClient(options);
});
services.AddHostedService<TeamTimeoutService>();
services.AddHostedService<PendingTeamRequestTimeoutService>();
services.AddHostedService<ReminderHostedService>();
services.AddSingleton<IConfiguration>(configuration);
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

services.AddDbContext<TaskManagementDbContext>(options =>
    TaskManagementDbContextConfiguration.Configure(options, configuration));
services.AddScoped<ITaskService, TaskService>();
services.AddScoped<ITeamService, TeamService>();
services.AddScoped<IProjectService, ProjectService>();
services.AddScoped<ITeamWorkflowService, TeamWorkflowService>();
services.AddScoped<IPendingTeamRequestService, PendingTeamRequestService>();
services.AddScoped<IReminderRepository, ReminderRepository>();
services.AddScoped<ReminderService>();
services.AddScoped<IReminderProcessor>(sp => sp.GetRequiredService<ReminderService>());
services.AddSingleton<IReminderNotificationSender, MezonReminderNotificationSender>();
services.AddScoped<IComplainRepository, ComplainRepository>();
services.AddScoped<IComplainService, ComplainService>();
services.AddSingleton<IMezonUserService, MezonUserService>();
services.AddScoped<SessionService>();
services.AddSingleton<ReportStateService>();

services.AddScoped<IBotService, BotService>();
services.AddScoped<ICommandHandler, TaskCommandHandler>();
services.AddScoped<ICommandHandler, TeamCommandHandler>();
services.AddScoped<IComponentHandler, TeamComponentHandler>();
services.AddScoped<IReportService, ReportService>();
services.AddScoped<ICommandHandler, ReportCommandHandler>();
services.AddScoped<IComponentHandler, ReportComponentHandler>();
services.AddScoped<IComponentHandler, TaskComponentHandler>();
services.AddScoped<ComplainCommandHandler>();
services.AddScoped<ICommandHandler, ComplainCommandHandler>();
services.AddScoped<IComponentHandler, ComplainComponentHandler>();

using var host = hostBuilder.Build();
var serviceProvider = host.Services;
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
using var cancellationSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellationSource.Cancel();
};

try
{
    logger.LogInformation("Connecting to database...");

    await using var scope = serviceProvider.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
    IBotService? botService = null;
    IReadOnlyList<IHostedService> hostedServices = [];

    await dbContext.Database.MigrateAsync(cancellationSource.Token);

    logger.LogInformation("Database connected and migrated successfully.");

    try
    {
        botService = scope.ServiceProvider.GetRequiredService<IBotService>();
        await botService.StartAsync(cancellationSource.Token);

        hostedServices = serviceProvider.GetServices<IHostedService>().ToList();
        await StartHostedServicesAsync(hostedServices, logger, cancellationSource.Token);

        logger.LogInformation("Bot is running. Press Ctrl+C to stop.");

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationSource.Token);
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
        await StopHostedServicesAsync(hostedServices, logger, CancellationToken.None);

        if (botService != null)
        {
            await botService.StopAsync(CancellationToken.None);
        }
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Database or bot startup failed");
    throw;
}

Console.WriteLine("Ready.");

static async Task StartHostedServicesAsync(
    IEnumerable<IHostedService> hostedServices,
    ILogger logger,
    CancellationToken cancellationToken)
{
    foreach (var hostedService in hostedServices)
    {
        logger.LogInformation("Starting hosted service {HostedService}", hostedService.GetType().Name);
        await hostedService.StartAsync(cancellationToken);
    }
}

static async Task StopHostedServicesAsync(
    IEnumerable<IHostedService> hostedServices,
    ILogger logger,
    CancellationToken cancellationToken)
{
    foreach (var hostedService in hostedServices.Reverse())
    {
        try
        {
            logger.LogInformation("Stopping hosted service {HostedService}", hostedService.GetType().Name);
            await hostedService.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop hosted service {HostedService}", hostedService.GetType().Name);
        }
    }
}
