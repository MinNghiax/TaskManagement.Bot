using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Services;

public interface IReportService
{
    Task<PersonalReportDto> GetPersonalReportAsync(
        string userId,
        string? clanId = null,
        string? channelId = null,
        string? threadId = null);
    Task<TeamReportDto> GetTeamReportAsync(
        string? clanId = null,
        string? channelId = null,
        string? threadId = null);
    Task<StatisticsReportDto> GetStatisticsReportAsync(
        ETimeRange timeRange,
        string? clanId = null,
        string? channelId = null,
        string? threadId = null);
    Task<List<DetailedTaskReportDto>> GetOverdueTasksAsync(
        string? clanId = null,
        string? channelId = null,
        string? threadId = null);
    Task<List<DetailedTaskReportDto>> GetProgressReportAsync(
        string? userId = null,
        string? clanId = null,
        string? channelId = null,
        string? threadId = null);
}