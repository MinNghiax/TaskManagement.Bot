using TaskManagement.Bot.Application.DTOs;

namespace TaskManagement.Bot.Application.Services;

public interface IReportService
{
    /// <summary>
    /// !report me - Báo cáo cá nhân cho MEMBER
    /// Hiển thị: Projects → Teams → Tasks của user
    /// </summary>
    Task<UserPersonalReportDto> GetUserPersonalReportAsync(string userId);

    /// <summary>
    /// !report - Báo cáo cho PM
    /// Trả về danh sách Projects mà PM tạo để chọn
    /// </summary>
    Task<PMProjectListDto> GetPMProjectsAsync(string pmUserId);

    /// <summary>
    /// Lấy danh sách Teams thuộc Project (cho PM chọn)
    /// </summary>
    Task<List<TeamSummaryDto>> GetTeamsByProjectAsync(int projectId);

    /// <summary>
    /// Lấy báo cáo chi tiết của Team (group theo Member)
    /// </summary>
    Task<TeamDetailReportDto> GetTeamDetailReportAsync(int teamId);

    /// <summary>
    /// !report @user - Báo cáo của 1 user cụ thể cho PM
    /// PM chỉ được xem báo cáo của user trong các Project mà PM tạo
    /// </summary>
    Task<UserReportByPMDto> GetUserReportByPMAsync(string targetUserId, string pmUserId);

    /// <summary>
    /// !report today/week/month - Báo cáo theo thời gian cho PM
    /// Filter tasks theo DueDate, group theo Member
    /// </summary>
    Task<TimeBasedReportDto> GetTimeBasedReportAsync(string pmUserId, TimeRangeFilter timeRange);
}

public enum TimeRangeFilter
{
    Today,
    Week,
    Month
}
