using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Helpers;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Commands.Report;

public class ReportCommandHandler : ICommandHandler
{
    private readonly ILogger<ReportCommandHandler> _logger;
    private readonly IReportService _reportService;
    private readonly IMezonUserService _userService;

    public ReportCommandHandler(
        ILogger<ReportCommandHandler> logger,
        IReportService reportService,
        IMezonUserService userService)
    {
        _logger = logger;
        _reportService = reportService;
        _userService = userService;
    }

    public bool CanHandle(string content)
    {
        return content.StartsWith("!report", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<CommandResponse> HandleAsync(ChannelMessage message, CancellationToken ct)
    {
        try
        {
            var content = message.Content?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new CommandResponse("Empty command");
            }

            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var userId = message.SenderId ?? "";
            var clanId = message.ClanId ?? "";

            _logger.LogInformation("[REPORT] Command: {Command} | User: {UserId}", content, userId);

            return parts.Length switch
            {
                1 => await HandleReportAsync(userId, clanId),
                2 when parts[1].Equals("me", StringComparison.OrdinalIgnoreCase) 
                    => await HandleReportMeAsync(userId),
                2 when IsTimeRangeCommand(parts[1]) 
                    => await HandleTimeBasedReportAsync(userId, ParseTimeRange(parts[1])),
                2 when parts[1].StartsWith("@") || parts[1].StartsWith("<@")
                    => await HandleReportUserAsync(userId, clanId, parts[1], message),
                _ => new CommandResponse(BuildUsageText())
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "[REPORT] Unauthorized access");
            return new CommandResponse($"❌ {ex.Message}");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "[REPORT] Not found");
            return new CommandResponse($"❌ {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REPORT] Error handling command");
            return new CommandResponse($"❌ Lỗi: {ex.Message}");
        }
    }

    private async Task<CommandResponse> HandleReportMeAsync(string userId)
    {
        _logger.LogInformation("[REPORT_ME] User: {UserId}", userId);

        var report = await _reportService.GetUserPersonalReportAsync(userId);
        var form = ReportFormBuilder.BuildUserPersonalReportForm(report);

        return new CommandResponse(form);
    }

    private async Task<CommandResponse> HandleReportAsync(string pmUserId, string clanId)
    {
        _logger.LogInformation("[REPORT_PM] PM: {PMUserId}", pmUserId);

        var report = await _reportService.GetPMProjectsAsync(pmUserId);
        var form = ReportFormBuilder.BuildPMProjectSelectionForm(report, clanId);

        return new CommandResponse(form);
    }

    private async Task<CommandResponse> HandleTimeBasedReportAsync(string pmUserId, TimeRangeFilter timeRange)
    {
        _logger.LogInformation("[REPORT_TIME] PM: {PMUserId} | Range: {TimeRange}", pmUserId, timeRange);

        var report = await _reportService.GetTimeBasedReportAsync(pmUserId, timeRange);
        var form = ReportFormBuilder.BuildTimeBasedReportForm(report);

        return new CommandResponse(form);
    }

    private async Task<CommandResponse> HandleReportUserAsync(string pmUserId, string clanId, string mentionToken, ChannelMessage message)
    {
        _logger.LogInformation("[REPORT_USER] PM: {PMUserId} | Mention: {Mention}", pmUserId, mentionToken);

        var targetUserId = UserHelper.ExtractUserIdFromMention(mentionToken, message);
        
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            return new CommandResponse("❌ Không tìm thấy user. Vui lòng mention user bằng @username");
        }

        var report = await _reportService.GetUserReportByPMAsync(targetUserId, pmUserId);
        var form = ReportFormBuilder.BuildUserReportByPMForm(report);

        return new CommandResponse(form);
    }

    private static bool IsTimeRangeCommand(string command)
    {
        return command.ToLowerInvariant() is "today" or "week" or "month";
    }

    private static TimeRangeFilter ParseTimeRange(string command)
    {
        return command.ToLowerInvariant() switch
        {
            "today" => TimeRangeFilter.Today,
            "week" => TimeRangeFilter.Week,
            "month" => TimeRangeFilter.Month,
            _ => TimeRangeFilter.Today
        };
    }

    private static string BuildUsageText()
    {
        return """
📊 **HƯỚNG DẪN SỬ DỤNG LỆNH REPORT**

**Cho MEMBER:**
• `!report me` - Xem báo cáo cá nhân (Projects → Teams → Tasks)

**Cho PM:**
• `!report` - Xem báo cáo team (chọn Project → Team)
• `!report @user` - Xem báo cáo của 1 user cụ thể
• `!report today` - Xem tasks có deadline hôm nay
• `!report week` - Xem tasks có deadline tuần này
• `!report month` - Xem tasks có deadline tháng này

**Lưu ý:**
- MEMBER chỉ được dùng `!report me`
- PM chỉ được xem báo cáo của các Project mình tạo
""";
    }
}
