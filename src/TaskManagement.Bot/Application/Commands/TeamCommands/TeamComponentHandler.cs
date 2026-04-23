using System.Text.Json;
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Microsoft.Extensions.Logging;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Commands.TeamCommands;

public class TeamComponentHandler : IComponentHandler
{
    private readonly ILogger<TeamComponentHandler> _logger;
    private readonly ITeamWorkflowService _workflowService;
    private readonly IProjectService _projectService;
    private readonly ITeamService _teamService;
    private readonly MezonClient _client;

    public TeamComponentHandler(
        ILogger<TeamComponentHandler> logger,
        ITeamWorkflowService workflowService,
        IProjectService projectService,
        ITeamService teamService,
        MezonClient client)
    {
        _logger = logger;
        _workflowService = workflowService;
        _projectService = projectService;
        _teamService = teamService;
        _client = client;
    }

    public bool CanHandle(string customId) =>
        customId.StartsWith("CREATE_TEAM", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("CANCEL_TEAM", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("ADD_MEMBER", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("ACCEPT", StringComparison.OrdinalIgnoreCase) ||
        customId.StartsWith("REJECT", StringComparison.OrdinalIgnoreCase);

    private ChannelMessage BuildOriginalMessage(ComponentContext context, string messageId, string? senderId)
    {
        var userIdToLookup = senderId ?? context.CurrentUserId;
        
        var user = _client.Clans.Get(context.ClanId!)?.Users.Get(userIdToLookup!);
        
        if (user != null)
        {
            _logger.LogInformation(
                "[BuildOriginalMessage] Found user in cache: {Username} ({UserId})",
                user.Username,
                userIdToLookup);
            
            return new ChannelMessage
            {
                Id = messageId,
                ChannelId = context.ChannelId!,
                ChannelLabel = "",
                SenderId = userIdToLookup,
                Username = user.Username,
                DisplayName = user.DisplayName,
                ClanNick = user.ClanNick,
                ClanAvatar = user.ClanAvatar,
                Content = new ChannelMessageContent { Text = "!team init" }, 
                ClanId = context.ClanId
            };
        }
        
        _logger.LogWarning(
            "[BuildOriginalMessage] User {UserId} not found in cache, using minimal message info",
            userIdToLookup);
        
        return new ChannelMessage
        {
            Id = messageId,
            ChannelId = context.ChannelId!,
            ChannelLabel = "",
            SenderId = userIdToLookup ?? "",
            Username = "",
            DisplayName = "",
            ClanNick = "",
            ClanAvatar = "",
            Content = new ChannelMessageContent { Text = "!team init" }, 
            ClanId = context.ClanId
        };
    }

    public async Task<ComponentResponse> HandleAsync(ComponentContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[TEAM_COMPONENT] CustomId={CustomId} ClanId={ClanId} ChannelId={ChannelId} UserId={UserId} MessageId={MessageId}",
            context.CustomId,
            context.ClanId,
            context.ChannelId,
            context.CurrentUserId,
            context.MessageId);

        if (string.IsNullOrWhiteSpace(context.ClanId) || string.IsNullOrWhiteSpace(context.ChannelId))
        {
            _logger.LogWarning("[TEAM_COMPONENT] Missing ClanId or ChannelId");
            return new ComponentResponse();
        }

        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (parts.Length == 0)
            return new ComponentResponse();

        var action = parts[0].ToUpperInvariant();
        
        if (action != "ACCEPT" && action != "REJECT")
        {
            var formOwnerId = ExtractFormOwnerId(parts, action);
            if (!string.IsNullOrWhiteSpace(formOwnerId) && formOwnerId != context.CurrentUserId)
            {
                // Hiển thị message nhưng không reply
                return ComponentResponse.FromText(
                    context.ClanId!,
                    context.ChannelId!,
                    "❌ Bạn không có quyền thao tác form này",
                    context.Mode,
                    context.IsPublic,
                    null, // Không reply
                    null  // Không có original message
                );
            }
        }
        
        return action switch
        {
            "CREATE_TEAM" => await HandleCreateTeamAsync(context, cancellationToken),
            "CANCEL_TEAM" => HandleCancel(context, "❌ Đã hủy tạo team."),
            "ADD_MEMBER" => HandleAddMember(context, parts),
            "ACCEPT" => await HandleAcceptAsync(context, parts, cancellationToken),
            "REJECT" => await HandleRejectAsync(context, parts, cancellationToken),
            _ => new ComponentResponse()
        };
    }

    private string? ExtractFormOwnerId(string[] parts, string action)
    {
        return action switch
        {
            "CREATE_TEAM" when parts.Length >= 3 => parts[2],
            "CANCEL_TEAM" when parts.Length >= 3 => parts[2],
            "ADD_MEMBER" when parts.Length >= 4 => parts[3],
            _ => null
        };
    }

    private async Task<ComponentResponse> HandleCreateTeamAsync(ComponentContext context, CancellationToken ct)

    {
        var projectName = ReadValue(context.Payload, "project_name");
        var teamName = ReadValue(context.Payload, "team_name");

        var formValues = new Dictionary<string, string>();
        for (var i = 1; i <= 6; i++)
        {
            var value = ReadValue(context.Payload, $"member_{i}");
            if (!string.IsNullOrWhiteSpace(value))
                formValues[$"member_{i}"] = value;
        }

        // Validate
        var (isValid, message, members) = TeamFormBuilder.ValidateForm(projectName, teamName, formValues);
        if (!isValid)
        {
            return ReplaceForm(context,
                TeamFormBuilder.BuildTeamFormWithError(message));
        }

        if (await _projectService.ExistsByNameAsync(projectName, ct))
            return BuildTextResponse(context, $"❌ Project `{projectName}` đã tồn tại");

        if (await _teamService.ExistsByNameAsync(teamName, ct))
            return BuildTextResponse(context, $"❌ Team `{teamName}` đã tồn tại");

        var resolvedMembers = await ResolveMembersAsync(context.ClanId!, members, ct);
        if (resolvedMembers.InvalidTokens.Count > 0)
            return BuildTextResponse(context, $"❌ Không tìm thấy user: {string.Join(", ", resolvedMembers.InvalidTokens)}");

        if (string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Không xác định được người tạo");

        var result = await _workflowService.CreateRequestAsync(new CreateTeamRequestInput
        {
            ProjectName = projectName,
            TeamName = teamName,
            PMUserId = context.CurrentUserId,
            Members = resolvedMembers.Members
        }, ct);

        if (!result.Success || string.IsNullOrWhiteSpace(result.RequestId))
            return BuildTextResponse(context, result.Message);

        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        string? originalMessageId = parts.Length >= 2 ? parts[^1] : null;
        string? originalSenderId = context.CurrentUserId; // Người tạo team 

        foreach (var member in result.Members)
        {
            try
            {
                await _client.SendEphemeralMessageAsync(
                    receiverId: member.UserId,
                    clanId: context.ClanId!,
                    channelId: context.ChannelId!,
                    mode: context.Mode,
                    isPublic: context.IsPublic,
                    content: TeamFormBuilder.BuildConfirmForm(result.RequestId, teamName, projectName, member.UserId, originalMessageId, originalSenderId),
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send invite to {UserId}", member.UserId);
            }
        }

        // Tạo response với cả message thông báo và xóa form
        var response = new ComponentResponse();

        // Xóa form hiện tại
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = context.MessageId
            });
        }

        // Gửi message thông báo kết quả
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = result.Message,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = context.MessageId
        });

        return response;
    }

    private ComponentResponse ReplaceForm(ComponentContext context, ChannelMessageContent content)
    {
        var formMessageId = context.MessageId ?? "";
        
        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        
        string? originalMessageId = null;
        
        if (parts.Length >= 2)
        {
            originalMessageId = parts[^1]; 
        }
        
        if (string.IsNullOrWhiteSpace(originalMessageId))
        {
            originalMessageId = formMessageId;
        }

        var response = new ComponentResponse();

        //  Xóa form cũ
        if (!string.IsNullOrWhiteSpace(formMessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = formMessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = null
            });
        }

        //  Gửi form mới - reply to original message
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = content,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId,
            OriginalMessage = BuildOriginalMessage(context, originalMessageId!, context.CurrentUserId)
        });

        return response;
    }

    private ComponentResponse HandleAddMember(ComponentContext context, string[] parts)
    {
        var currentCount = parts.Length > 1 && int.TryParse(parts[1], out var c) ? c : 1;

        if (currentCount >= 6)
            return BuildTextResponse(context, "❌ Đã đạt giới hạn 6 thành viên");

        string? originalMessageId = parts.Length >= 3 ? parts[2] : null;
        string? senderId = parts.Length >= 4 ? parts[3] : null;

        var response = new ComponentResponse();

        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = null
            });
        }

        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Content = TeamFormBuilder.BuildTeamForm(currentCount + 1, originalMessageId, senderId),
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId ?? context.MessageId,
            OriginalMessage = BuildOriginalMessage(context, originalMessageId ?? context.MessageId!, senderId)
        });

        return response;
    }

    private async Task<ComponentResponse> HandleAcceptAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 1 || string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Yêu cầu không hợp lệ");

        var result = await _workflowService.AcceptAsync(parts[1], parts[2], context.CurrentUserId, context.ClanId!, ct);
        return HandleCancel(context, result.Message);
    }

    private async Task<ComponentResponse> HandleRejectAsync(ComponentContext context, string[] parts, CancellationToken ct)
    {
        if (parts.Length < 1 || string.IsNullOrWhiteSpace(context.CurrentUserId))
            return BuildTextResponse(context, "❌ Yêu cầu không hợp lệ");

        var result = await _workflowService.RejectAsync(parts[1], parts[2], context.CurrentUserId, context.ClanId!, ct);
        return HandleCancel(context, result.Message);
    }

    private ComponentResponse HandleCancel(ComponentContext context, string? message = null)
    {
        var response = new ComponentResponse();

        string? originalMessageId = null;
        string? originalSenderId = null;
        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        
        if (parts.Length >= 5 && (parts[0] == "ACCEPT" || parts[0] == "REJECT"))
        {
            originalMessageId = parts[3]; 
            originalSenderId = parts[4];  
        }
        else if (parts.Length >= 2)
        {
            originalMessageId = parts[^1]; 
            originalSenderId = context.CurrentUserId; 
        }

        // lấy đúng message chứa form
        var formMessageId = context.MessageId;

        if (!string.IsNullOrWhiteSpace(formMessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = formMessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = null
            });
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            response.Messages.Add(new ComponentMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                Text = message,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = originalMessageId,
                OriginalMessage = !string.IsNullOrWhiteSpace(originalMessageId) 
                    ? BuildOriginalMessage(context, originalMessageId, originalSenderId)
                    : null
            });
        }

        return response;
    }

    private ComponentResponse BuildTextResponse(ComponentContext context, string text)
    {
        //  LẤY originalMessageId từ customId
        var parts = context.CustomId?.Split('|', StringSplitOptions.RemoveEmptyEntries) ?? [];
        
        string? originalMessageId = null;
        
        if (parts.Length >= 2)
        {
            originalMessageId = parts[^1]; 
        }
        
        if (string.IsNullOrWhiteSpace(originalMessageId))
        {
            originalMessageId = context.MessageId;
        }

        var response = new ComponentResponse();
        
        if (!string.IsNullOrWhiteSpace(context.MessageId))
        {
            response.DeleteMessages.Add(new ComponentDeleteMessage
            {
                ClanId = context.ClanId!,
                ChannelId = context.ChannelId!,
                MessageId = context.MessageId,
                Mode = context.Mode,
                IsPublic = context.IsPublic,
                ReplyToMessageId = null
            });
        }
        
        response.Messages.Add(new ComponentMessage
        {
            ClanId = context.ClanId!,
            ChannelId = context.ChannelId!,
            Text = text,
            Mode = context.Mode,
            IsPublic = context.IsPublic,
            ReplyToMessageId = originalMessageId,
            OriginalMessage = BuildOriginalMessage(context, originalMessageId ?? context.MessageId!, context.CurrentUserId)
        });

        return response;
    }

    private async Task<ResolvedMembers> ResolveMembersAsync(string clanId, List<string> memberTokens, CancellationToken ct)
    {
        var resolved = new List<TeamRequestMember>();
        var invalid = new List<string>();

        var users = _client.Clans.Get(clanId)?.Users.GetAll();

        _logger.LogInformation("========== DEBUG MEMBERS ==========");
        _logger.LogInformation("Total users in clan: {Count}", users?.Count ?? 0);

        if (users != null)
        {
            foreach (var u in users)
            {
                _logger.LogInformation(
                    "UserId={Id} | Username={Username} | DisplayName={Display} | ClanNick={Nick}",
                    u.Id,
                    u.Username,
                    u.DisplayName,
                    u.ClanNick
                );
            }
        }

        foreach (var token in memberTokens.Distinct())
        {
            _logger.LogInformation("---- Checking token ----");
            _logger.LogInformation("Raw token: {Token}", token);

            if (long.TryParse(token, out _))
            {
                _logger.LogInformation("Detected as UserId: {Token}", token);

                resolved.Add(new TeamRequestMember
                {
                    UserId = token,
                    Handle = $"<@{token}>"
                });

                continue;
            }

            var normalized = token.Trim().TrimStart('@');

            _logger.LogInformation("Normalized token: {Normalized}", normalized);

            var user = users?.FirstOrDefault(u =>
            {
                var isMatch =
                    string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.DisplayName, normalized, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.ClanNick, normalized, StringComparison.OrdinalIgnoreCase);

                if (isMatch)
                {
                    _logger.LogInformation(
                        "MATCH FOUND → Token={Token} matched UserId={Id} Username={Username}",
                        normalized,
                        u.Id,
                        u.Username
                    );
                }

                return isMatch;
            });

            if (user != null)
            {
                resolved.Add(new TeamRequestMember
                {
                    UserId = user.Id,
                    Handle = $"@{normalized}"
                });
            }
            else
            {
                _logger.LogWarning("❌ NOT FOUND: {Token}", normalized);
                invalid.Add(token);
            }
        }

        _logger.LogInformation("Resolved count: {Count}", resolved.Count);
        _logger.LogInformation("Invalid count: {Count}", invalid.Count);

        return new ResolvedMembers(resolved, invalid);
    }

    private static string ReadValue(JsonElement payload, string key)
    {
        var valuesNode = ComponentPayloadHelper.GetValues(payload);
        var element = ComponentPayloadHelper.GetPropertyIgnoreCase(valuesNode, key);

        if (element.HasValue)
        {
            var el = element.Value;

            // dạng string trực tiếp
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString() ?? string.Empty;

            // dạng object { value: "xxx" }
            if (el.ValueKind == JsonValueKind.Object &&
                el.TryGetProperty("value", out var valueProp))
            {
                return valueProp.GetString() ?? string.Empty;
            }
        }

        var extraData = ComponentPayloadHelper.GetExtraData(payload);

        if (!string.IsNullOrWhiteSpace(extraData) && extraData.TrimStart().StartsWith("{"))
        {
            try
            {
                using var json = JsonDocument.Parse(extraData);
                var extraElement = ComponentPayloadHelper.GetPropertyIgnoreCase(json.RootElement, key);

                if (extraElement.HasValue)
                {
                    var el = extraElement.Value;

                    if (el.ValueKind == JsonValueKind.String)
                        return el.GetString() ?? string.Empty;

                    if (el.ValueKind == JsonValueKind.Object &&
                        el.TryGetProperty("value", out var valueProp))
                    {
                        return valueProp.GetString() ?? string.Empty;
                    }
                }
            }
            catch { }
        }

        return string.Empty;
    }

    private sealed record ResolvedMembers(IReadOnlyList<TeamRequestMember> Members, IReadOnlyList<string> InvalidTokens);
}
