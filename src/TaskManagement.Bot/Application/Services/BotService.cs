using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services;

public interface IBotService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task SendMessageAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        CancellationToken cancellationToken = default);
}

public class BotService : IBotService
{
    private readonly ILogger<BotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<ICommandHandler> _handlers;
    private readonly Dictionary<string, PendingTeamRequest> _pendingRequests = new();
    private readonly HashSet<string> _handledClicks = new();
    private MezonClient? _client;
    private HashSet<string> _dmChannelIds = new();

    public BotService(
        ILogger<BotService> logger,
        IConfiguration configuration,
        IEnumerable<ICommandHandler> handlers)
    {
        _logger = logger;
        _configuration = configuration;
        _handlers = handlers;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🤖 Starting Mezon Bot...");
        var options = new MezonClientOptions
        {
            BotId = _configuration["Mezon:BotId"] ?? throw new Exception("Missing BotId"),
            Token = _configuration["Mezon:Token"] ?? throw new Exception("Missing Token"),
            Host = _configuration["Mezon:Host"] ?? "gw.mezon.ai",
            Port = _configuration["Mezon:Port"] ?? "443",
            UseSSL = bool.Parse(_configuration["Mezon:UseSsl"] ?? "true"),
            TimeoutMs = int.Parse(_configuration["Mezon:TimeoutMs"] ?? "10000")
        };
        _client = new MezonClient(options);
        _client.On("channel_message", OnChannelMessage);
        _client.On("message_component", OnComponent);
        await _client.LoginAsync(cancellationToken);
        try
        {
            var dmChannels = await _client.ListChannelsAsync(channelType: 1, cancellationToken: cancellationToken);
            _dmChannelIds = dmChannels.ChannelDescs != null
                ? new HashSet<string>(dmChannels.ChannelDescs.Select(c => c.ChannelId ?? ""))
                : new HashSet<string>();
            _logger.LogInformation($"[DM CACHE] Cached {_dmChannelIds.Count} DM channel ids");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[DM CACHE] Failed to cache DM channel ids");
        }
        _logger.LogInformation("✅ Bot connected & ready!");
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_client != null)
        {
            _logger.LogInformation("🔌 Stopping bot...");
            await _client.LogoutAsync(cancellationToken);
        }
    }

    public async Task SendMessageAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        CancellationToken cancellationToken = default)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️ Client not ready");
            return;
        }

        try
        {
            var finalIsPublic = mode == 4 ? false : isPublic;

            _logger.LogInformation($"[SEND] ClanId={clanId} ChannelId={channelId} Mode={mode} IsPublic={finalIsPublic}");

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: new ChannelMessageContent { Text = text },
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("✅ Sent!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Send failed");
            throw;
        }
    }

    private async Task SendMessageWithEmbedAsync(
        string clanId,
        string channelId,
        string text,
        int mode,
        bool isPublic,
        IInteractiveMessageProps embed)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️ Client not ready");
            return;
        }

        try
        {
            var finalIsPublic = mode == 4 ? false : isPublic;

            _logger.LogInformation($"[SEND_EMBED] ClanId={clanId} ChannelId={channelId} Mode={mode} IsPublic={finalIsPublic}");

            var content = new ChannelMessageContent
            {
                Text = text ?? "",
                Embed = new object[] { embed }
            };

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: content,
                cancellationToken: CancellationToken.None
            );

            _logger.LogInformation("✅ Sent with embed!");
        }
        catch (Exception ex) when (ex.Message.Contains("Invalid channel identifier") && mode == 2)
        {
            _logger.LogWarning($"[RETRY] Got 'Invalid channel identifier' with mode=2, retrying with mode=3 (private group)...");
            try
            {
                var content = new ChannelMessageContent
                {
                    Text = string.IsNullOrWhiteSpace(text) ? "📊 Report" : text,
                    Embed = new object[] { embed }
                };

                await _client.SendMessageAsync(
                    clanId: clanId,
                    channelId: channelId,
                    mode: 3,
                    isPublic: false,
                    content: content,
                    cancellationToken: CancellationToken.None
                );

                _logger.LogInformation("✅ Sent with embed (mode=3 private group)!");
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "❌ Send with embed (retry mode=3) failed");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Send with embed failed");
            throw;
        }
    }

    private async Task SendFormMessageAsync(
        string clanId,
        string channelId,
        ChannelMessageContent content,
        int mode,
        bool isPublic)
    {
        if (_client == null)
        {
            _logger.LogWarning("⚠️ Client not ready");
            return;
        }

        try
        {
            var finalIsPublic = mode == 4 ? false : isPublic;

            _logger.LogInformation($"[SEND_FORM] ClanId={clanId} ChannelId={channelId} Mode={mode} IsPublic={finalIsPublic}");

            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: finalIsPublic,
                content: content,
                cancellationToken: CancellationToken.None
            );

            _logger.LogInformation("✅ Sent interactive form!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Send form failed");
            throw;
        }
    }

    private async void OnChannelMessage(object? sender, Mezon.Sdk.Interfaces.MezonEventArgs e)
    {
        try
        {
            if (_client == null) return;
            if (e.Data is not ChannelMessage message) return;

            _logger.LogInformation($"📦 MESSAGE FULL: ClanId={message.ClanId} ChannelId={message.ChannelId} Mode={message.Mode} IsPublic={message.IsPublic} Sender={message.SenderId}");

            var rawText = message.Content?.Text;
            _logger.LogInformation($"📥 RAW: {rawText}");

            var content = ParseContent(rawText);
            _logger.LogInformation($"📥 PARSED: {content}");

            if (string.IsNullOrWhiteSpace(content)) return;
            if (message.SenderId == _client.ClientId) return;

            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(content))
                {
                    var response = await handler.HandleAsync(message, CancellationToken.None);

                    if (response == null || (string.IsNullOrEmpty(response.Text) && response.Embed == null && response.Content == null))
                        break;
                    var finalMode = message.Mode ?? 2;
                    var finalIsPublic = message.IsPublic ?? true;

                    _logger.LogInformation($"[SEND_MODE] Mode={message.Mode}→{finalMode} IsPublic={message.IsPublic}→{finalIsPublic}");

                    if (response.Content != null)
                    {
                        await SendFormMessageAsync(
                            message.ClanId!,
                            message.ChannelId!,
                            response.Content,
                            finalMode,
                            finalIsPublic
                        );
                    }
                    else if (response.Embed != null)
                    {
                        await SendMessageWithEmbedAsync(
                            message.ClanId!,
                            message.ChannelId!,
                            response.Text ?? "",
                            finalMode,
                            finalIsPublic,
                            response.Embed
                        );
                    }
                    else
                    {
                        await SendMessageAsync(
                            message.ClanId!,
                            message.ChannelId!,
                            response.Text ?? "",
                            finalMode,
                            finalIsPublic
                        );
                    }

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Handle message error");
        }
    }

    private string? ParseContent(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (raw.StartsWith("{"))
        {
            try
            {
                var json = JsonDocument.Parse(raw);
                return json.RootElement.GetProperty("t").GetString();
            }
            catch
            {
                _logger.LogWarning("⚠️ Parse JSON failed");
            }
        }
        return raw;
    }

    private async void OnComponent(object? sender, Mezon.Sdk.Interfaces.MezonEventArgs e)
    {
        try
        {
            if (_client == null) return;

            dynamic data = e.Data;

            string? customId = data?.Data?.CustomId;
            string? clanId = data?.ClanId;
            string? channelId = data?.ChannelId;

            _logger.LogInformation($"[BUTTON CLICK] {customId}");

            if (string.IsNullOrEmpty(customId)) return;

            var parts = customId.Split('|');

            switch (parts[0])
            {
                case "CREATE_TEAM":
                    await HandleCreateTeam(data);
                    break;

                case "CANCEL_TEAM":
                    await SendMessageAsync(clanId, channelId, "❌ Đã huỷ", 2, true);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Handle component error");
        }
    }

    private async Task HandleCreateTeam(dynamic data)
    {
        try
        {
            var values = data.Data.Values;

            string projectName = values["project_name"];
            string teamName = values["team_name"];
            string members = values["members"];

            _logger.LogInformation($"PROJECT={projectName} TEAM={teamName} MEMBERS={members}");

            var (isValid, message) = TaskManagement.Bot.Application.Commands.TaskCommands.TaskFormBuilder.ValidateForm(
                projectName,
                teamName,
                "PM",
                members
            );

            if (!isValid)
            {
                await SendMessageAsync(data.ClanId, data.ChannelId, message, 2, true);
                return;
            }

            //  gọi service thật ở đây
            await SendMessageAsync(
                data.ClanId,
                data.ChannelId,
                $"✅ Tạo team `{teamName}` thành công!",
                2,
                true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Create team failed");
        }
    }

    private async void OnButtonClicked(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data is not Envelope envelope || envelope.MessageButtonClicked == null)
                return;

            var data = envelope.MessageButtonClicked;
            //  chống click 2 lần
            var uniqueKey = $"{data.MessageId}_{data.UserId}_{data.ButtonId}";

            if (_handledClicks.Contains(uniqueKey))
            {
                _logger.LogWarning("⚠️ Duplicate click ignored");
                return;
            }

            _handledClicks.Add(uniqueKey);

            var buttonId = data.ButtonId;

            var parts = data.ButtonId.Split('|');

            var action = parts[0];

            var channelId = data.ChannelId.ToString();

            string clanId = "";

            var messageId = data.MessageId.ToString();
            string senderId = data.UserId.ToString();

            //  Ưu tiên map từ message trước đó
            if (_channelClanMap.TryGetValue(channelId, out var mappedClanId))
            {
                clanId = mappedClanId;
                _logger.LogInformation("✅ clanId từ map");
            }
            else if (!string.IsNullOrEmpty(_lastClanId))
            {
                clanId = _lastClanId;
                _logger.LogWarning("⚠️ fallback clanId từ last message");
            }

            _logger.LogInformation($"👉 FIX clanId = {clanId}");
            _logger.LogInformation($"👉 channelId = {channelId}");

            if (string.IsNullOrEmpty(clanId))
            {
                _logger.LogError("❌ clanId missing");
                return;
            }

            _logger.LogInformation($"👉 clanId = {clanId}");
            _logger.LogInformation($"👉 channelId = {channelId}");

            if (string.IsNullOrEmpty(clanId))
            {
                _logger.LogError("❌ Không tìm được clanId từ SDK cache");
                return;
            }

            //  CANCEL
            if (action == "CANCEL_TEAM")
            {

                //  Reply message
                await _client.SendMessageAsync(
                    clanId: clanId,
                    channelId: channelId,
                    mode: 2,
                    isPublic: true,
                    content: new ChannelMessageContent
                    {
                        Text = "❌ Đã huỷ nhập team"
                    },
                    references: new[]
                    {
                        new ApiMessageRef
                        {
                            MessageId = messageId,
                            MessageRefId = messageId,
                            MessageSenderId = senderId,
                            RefType = 0

                        }
                    }
                );

                return;
            }

            //  CREATE
            if (action == "CREATE_TEAM")
            {
                var json = JsonDocument.Parse(data.ExtraData);
                var root = json.RootElement;

                var projectName = root.GetProperty("project_name").GetString();
                var teamName = root.GetProperty("team_name").GetString();
                var membersRaw = root.GetProperty("members").GetString();

                var role = "PM";

                //  VALIDATE
                var validate = TaskFormBuilder.ValidateForm(
                    projectName!,
                    teamName!,
                    role,
                    membersRaw!
                );

                if (!validate.isValid)
                {
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = validate.message
                        }
                    );
                    return;
                }

                //  chuẩn hóa dữ liệu
                projectName = projectName!.Trim();
                teamName = teamName!.Trim();

                //  CHECK TRÙNG PROJECT
                var existProject = await _context.Projects
                    .FirstOrDefaultAsync(x => x.Name.ToLower() == projectName.ToLower());

                if (existProject != null)
                {
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = $"❌ Project `{projectName}` đã tồn tại"
                        }
                    );
                    return;
                }

                //  CHECK TRÙNG TEAM
                var existTeam = await _context.Teams
                    .FirstOrDefaultAsync(x => x.Name.ToLower() == teamName.ToLower());

                if (existTeam != null)
                {
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = $"❌ Team `{teamName}` đã tồn tại"
                        }
                    );
                    return;
                }

                //  PM = người bấm nút
                var pmUserId = data.UserId.ToString();

                //  parse members
                var memberUsernames = membersRaw!
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Replace("@", "").Trim().ToLower())
                    .Distinct()
                    .ToList();

                var memberUserIds = new List<string>();

                foreach (var username in memberUsernames)
                {
                    if (_userCache.TryGetValue(username, out var id))
                    {
                        memberUserIds.Add(id);
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ Không tìm thấy user: {username}");
                    }
                }

                //  CREATE TEAM
                var requestId = Guid.NewGuid().ToString();

                //  LƯU message form (message chứa button)
                var formMessageId = messageId;

                _pendingRequests[requestId] = new PendingTeamRequest
                {
                    ProjectName = projectName!,
                    TeamName = teamName!,
                    PMUserId = pmUserId,
                    MemberUserIds = memberUserIds,
                    AcceptedUserIds = new List<string>(),
                    CreatedAt = DateTime.UtcNow,
                    MessageId = formMessageId,
                    SenderId = data.UserId.ToString()
                };

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(30));

                    if (_pendingRequests.ContainsKey(requestId))
                    {
                        _pendingRequests.Remove(requestId);

                        await _client.SendMessageAsync(
                            clanId,
                            channelId,
                            2,
                            true,
                            new ChannelMessageContent
                            {
                                Text = $"❌ Team `{teamName}` đã bị huỷ do không đủ xác nhận"
                            }
                        );
                    }
                });

                var clan = _client.Clans.Get(clanId);
                if (clan == null) return;

                var allUserIds = new List<string> { pmUserId };
                allUserIds.AddRange(memberUserIds);

                //  SEND DM
                foreach (var targetUserId in allUserIds.Distinct())
                {
                    if (targetUserId == pmUserId) continue;

                    var user = clan.Users.Cache
                        .FirstOrDefault(x => x.Value.Id == targetUserId)
                        .Value;

                    if (user == null) continue;

                    //  GỬI DM (thông báo riêng)
                    await user.SendDMAsync(
                        new ChannelMessageContent
                        {
                            Text = $"📩 Bạn được mời vào team `{teamName}`\n👉 Vào channel để xác nhận"
                        }
                    );

                    //  GỬI CHANNEL (có button)
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        TaskFormBuilder.BuildConfirmForm(
                            requestId,
                            teamName!,
                            projectName!,
                            targetUserId
                        )
                    );
                }

                //  RESPONSE
                await _client.SendMessageAsync(
                    clanId,
                    channelId,
                    2,
                    true,
                    new ChannelMessageContent
                    {
                        Text = $"📩 Đã gửi yêu cầu tạo team `{teamName}`. Vui lòng chờ member xác nhận."
                    },
                    references: new[]
                    {
                        new ApiMessageRef
                        {
                            MessageId = messageId,
                            MessageRefId = messageId,
                            MessageSenderId = senderId,
                            RefType = 0
                        }
                    }
                );
            }

            if (action == "ACCEPT")
            {
                var requestId = parts[1];
                var userId = parts[2];
                var currentUserId = data.UserId.ToString();

                //  check quyền
                if (currentUserId != userId)
                {
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = "❌ Bạn không có quyền bấm nút này"
                        }
                    );
                    return;
                }

                _logger.LogInformation($"✅ ACCEPT CLICK: {userId}");

                if (!_pendingRequests.ContainsKey(requestId))
                {
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = "❌ Request đã hết hạn"
                        }
                    );
                    return;
                }

                var request = _pendingRequests[requestId];
                //  dùng message gốc
                messageId = request.MessageId;
                senderId = request.SenderId;

                if (!request.AcceptedUserIds.Contains(userId))
                {
                    request.AcceptedUserIds.Add(userId);
                }

                //  đủ người → tạo team
                if (request.AcceptedUserIds.Count == request.MemberUserIds.Count)
                {
                    await _teamService.CreateTeamAsync(
                        1,
                        request.TeamName,
                        request.PMUserId,
                        request.MemberUserIds
                    );

                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = $"🎉 Team `{request.TeamName}` đã tạo thành công!"
                        },
                        references: new[]
                        {
                            new ApiMessageRef
                            {
                                MessageId = messageId,
                                MessageRefId = messageId,
                                MessageSenderId = senderId,
                                RefType = 0
                            }
                        }
                    );

                    _pendingRequests.Remove(requestId);
                }
                else
                {
                    await _client.SendMessageAsync(
                        clanId,
                        channelId,
                        2,
                        true,
                        new ChannelMessageContent
                        {
                            Text = $"👍 <@{userId}> đã xác nhận"
                        },
                        references: new[]
                        {
                            new ApiMessageRef
                            {
                                MessageId = messageId,
                                MessageRefId = messageId,
                                MessageSenderId = senderId,
                                RefType = 0
                            }
                        }
                    );
                }
            }

            if (action == "REJECT")
            {
                var requestId = parts[1];
                var userId = parts[2];
                var currentUserId = data.UserId.ToString();

                if (currentUserId != userId)
                    return;

                if (!_pendingRequests.ContainsKey(requestId))
                    return;

                var request = _pendingRequests[requestId];

                //  LẤY message gốc
                messageId = request.MessageId;
                senderId = request.SenderId;
                _pendingRequests.Remove(requestId);

                await _client.SendMessageAsync(
                    clanId,
                    channelId,
                    2,
                    true,
                    new ChannelMessageContent
                    {
                        Text = $"❌ <@{userId}> đã từ chối. Team bị huỷ."
                    },
                    references: new[]
                    {
                        new ApiMessageRef
                        {
                            MessageId = messageId,
                            MessageRefId = messageId,
                            MessageSenderId = senderId,
                            RefType = 0
                        }
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Button error");
        }
    }

    private async Task ReplyAsync(Mezon.Sdk.Domain.ChannelMessage message, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var references = new[]
        {
        new ApiMessageRef
        {
            MessageId = message.MessageId,
            MessageRefId = message.MessageId,
            MessageSenderId = message.SenderId.ToString(),
            RefType = 0
        }
    };

        await _client.SendMessageAsync(
            clanId: message.ClanId,
            channelId: message.ChannelId,
            mode: message.Mode ?? 2,
            isPublic: message.IsPublic ?? true,
            content: new ChannelMessageContent
            {
                Text = text
            },
            references: references
        );
    }
}
