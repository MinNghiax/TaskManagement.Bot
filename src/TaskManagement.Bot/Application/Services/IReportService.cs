using TaskManagement.Bot.Application.DTOs;

namespace TaskManagement.Bot.Application.Services;

public interface IReportService
{
    Task<UserPersonalReportDto> GetUserPersonalReportAsync(string userId);
    Task<PMProjectListDto> GetPMProjectsAsync(string pmUserId);
    Task<List<TeamSummaryDto>> GetTeamsByProjectAsync(int projectId);
    Task<TeamDetailReportDto> GetTeamDetailReportAsync(int teamId);
}
