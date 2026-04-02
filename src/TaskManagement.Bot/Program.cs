using TaskManagement.Bot.Features.Task.Commands;
using TaskManagement.Bot.Features.Task.Services;
using TaskManagement.Bot.Features.Task.Persistence;
using TaskManagement.Bot.Features.TaskQuery.Services;
using TaskManagement.Bot.Features.TaskQuery.Persistence;
using TaskManagement.Bot.Features.TaskQuery.Commands;
using TaskManagement.Bot.Features.Reminder.Commands;
using TaskManagement.Bot.Features.Reminder.Services;
using TaskManagement.Bot.Features.Reminder.Persistence;
using TaskManagement.Bot.Features.ThreadContext.Commands;
using TaskManagement.Bot.Features.ThreadContext.Services;
using TaskManagement.Bot.Features.ThreadContext.Persistence;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((ctx, services) =>
{
    // Shared
    // TODO: Add DbContext here
    
    // Feature 1: Task Management (Người 1)
    services.AddScoped<ITaskRepository, TaskRepository>();
    services.AddScoped<ITaskService, TaskService>();
    services.AddScoped<TaskCreateCommand>();
    // TODO: Add other Task commands here
    
    // Feature 2: Task Search/Query (Người 2)
    services.AddScoped<ITaskSearchService, TaskSearchService>();
    services.AddScoped<ITaskQueryRepository, TaskQueryRepository>();
    services.AddScoped<TaskListCommand>();
    // TODO: Add other TaskQuery commands here
    
    // Feature 3: Reminders (Người 3)
    services.AddScoped<IReminderRepository, ReminderRepository>();
    services.AddScoped<IReminderService, ReminderService>();
    services.AddScoped<ReminderSetCommand>();
    // TODO: Add other Reminder commands here
    
    // Feature 4: Thread Context (Người 4)
    services.AddScoped<ITaskContextRepository, TaskContextRepository>();
    services.AddScoped<IThreadContextService, ThreadContextService>();
    services.AddScoped<TaskCreateHereCommand>();
    // TODO: Add other ThreadContext commands here
    
    // TODO: Add Mezon.Sdk bot initialization here
    // TODO: Add event handlers registration here
});

var host = builder.Build();

Console.WriteLine("TaskManagement.Bot - Starting...");
Console.WriteLine("✅ Features loaded:");
Console.WriteLine("   • Feature 1: Task Management (Người 1)");
Console.WriteLine("   • Feature 2: Task Search (Người 2)");
Console.WriteLine("   • Feature 3: Reminders (Người 3)");
Console.WriteLine("   • Feature 4: Thread Context (Người 4)");
Console.WriteLine("");
Console.WriteLine("📂 Structure: Features/* for isolated development");
Console.WriteLine("             Shared/* for cross-feature utilities");
Console.WriteLine("");

// TODO: Uncomment when bot integration is ready
// await host.RunAsync();
