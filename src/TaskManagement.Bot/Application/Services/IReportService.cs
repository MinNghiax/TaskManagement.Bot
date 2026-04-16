using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public interface IReportService
{
    // ===== EXISTING METHODS =====
    Task<PersonalReportDto> GetPersonalReportAsync(
        string userId,
        string? clanId = null,
        string? channelId = null);

    Task<TeamReportDto> GetTeamReportAsync(
        string? clanId = null,
        string? channelId = null);

    Task<StatisticsReportDto> GetStatisticsReportAsync(
        ETimeRange timeRange,
        string? clanId = null,
        string? channelId = null);

    Task<List<DetailedTaskReportDto>> GetOverdueTasksAsync(
        string? clanId = null,
        string? channelId = null);

    Task<List<DetailedTaskReportDto>> GetProgressReportAsync(
        string? userId = null,
        string? clanId = null,
        string? channelId = null);

    // ===== NEW COMPREHENSIVE METHODS =====

    /// <summary>
    /// Get comprehensive report for a single task with all related information:
    /// task details, team/project context, reminders, complaints, health metrics
    /// </summary>
    Task<ComprehensiveTaskReportDto> GetComprehensiveTaskReportAsync(int taskId);

    /// <summary>
    /// Get enhanced personal report showing all tasks with health status tracking
    /// </summary>
    Task<EnhancedPersonalTaskReportDto> GetEnhancedPersonalReportAsync(
        string userId,
        string? clanId = null,
        string? channelId = null);

    /// <summary>
    /// Get team health report - overall team metrics and member performance
    /// </summary>
    Task<TeamHealthReportDto> GetTeamHealthReportAsync(int teamId);

    /// <summary>
    /// Get task analytics - productivity and velocity metrics for time period
    /// </summary>
    Task<TaskAnalyticsReportDto> GetTaskAnalyticsReportAsync(
        ETimeRange timeRange,
        string? clanId = null,
        string? channelId = null);

    /// <summary>
    /// Find tasks by various criteria for dashboard/filtering
    /// </summary>
    Task<List<ComprehensiveTaskReportDto>> FindTasksAsync(
        string? status = null,
        string? priority = null,
        string? assignedTo = null,
        string? createdBy = null,
        bool? onlyOverdue = false,
        bool? onlyAtRisk = false);
}
