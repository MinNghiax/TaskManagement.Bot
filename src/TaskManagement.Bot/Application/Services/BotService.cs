
using Mezon.Sdk;
using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;
using Mezon.Sdk.Interfaces;
using Mezon.Sdk.Proto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TaskManagement.Bot.Application.Commands;
using TaskManagement.Bot.Application.Commands.TaskCommands;
using TaskManagement.Bot.Infrastructure.Data;
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
    private readonly ITeamService _teamService;
    private readonly TaskCommandHandler _commandHandler;
    private readonly TaskManagementDbContext _context;
    private MezonClient? _client;
    private HashSet<string> _dmChannelIds = new();
    private string _lastClanId = "";
    private Dictionary<string, string> _channelClanMap = new();
    private Dictionary<string, string> _userCache = new();
    //private Dictionary<string, string> _teamTemp = new();

    public BotService(
        ILogger<BotService> logger,
        IConfiguration configuration,
        IEnumerable<ICommandHandler> handlers,
        TaskCommandHandler commandHandler, 
        MezonClient client,
        ITeamService teamService, 
        TaskManagementDbContext context)
    {
        _logger = logger;
        _configuration = configuration;
        _commandHandler = commandHandler;
        _client = client;
        _context = context;
        _teamService = teamService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🤖 Starting Mezon Bot...");
        var options = new MezonClientOptions
        {
            BotId = _configuration["Mezon:BotId"]! ?? throw new Exception("Missing BotId"),
            Token = _configuration["Mezon:Token"]! ?? throw new Exception("Missing Token"),
            Host = _configuration["Mezon:Host"] ?? "gw.mezon.ai",
            Port = _configuration["Mezon:Port"] ?? "443",
            UseSSL = bool.Parse(_configuration["Mezon:UseSsl"] ?? "true"),
            TimeoutMs = int.Parse(_configuration["Mezon:TimeoutMs"] ?? "10000")
        };
        _client = new MezonClient(options);

        // listen message
        _client.On("channel_message", OnChannelMessage);

        _client.On(MezEvent.MessageButtonClicked, OnButtonClicked);
        await _client.LoginAsync(cancellationToken);
        // Cache DM channel ids
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
            await _client.SendMessageAsync(
                clanId: clanId,
                channelId: channelId,
                mode: mode,
                isPublic: isPublic,
                content: new ChannelMessageContent { Text = text },
                cancellationToken: cancellationToken
            );
            _logger.LogInformation("✅ Sent!");
        }
        catch (Exception ex)
        {
            // Nếu lỗi là Invalid channel identifier thì thử lại bằng SendDMAsync
            if (ex.Message.Contains("Invalid channel identifier"))
            {
                _logger.LogWarning("[Fallback] Invalid channel identifier, retrying with SendDMAsync...");
                await _client.SendDMAsync(
                    channelDmId: channelId,
                    message: text,
                    cancellationToken: cancellationToken
                );
                _logger.LogInformation("✅ Sent via SendDMAsync!");
            }
            else
            {
                _logger.LogError(ex, "❌ Send failed");
                throw;
            }
        }
    }

    //private async void OnChannelMessage(object? sender, Mezon.Sdk.Interfaces.MezonEventArgs e)
    //{
    //    try
    //    {
    //        if (_client == null) return;
    //        if (e.Data is not ChannelMessage message) return;
    //        _logger.LogInformation($"📦 MESSAGE FULL: ClanId: {message.ClanId} ChannelId: {message.ChannelId} Mode: {message.Mode} IsPublic: {message.IsPublic} Sender: {message.SenderId}");
    //        var rawText = message.Content?.Text;
    //        _logger.LogInformation($"📥 RAW: {rawText}");
    //        var content = ParseContent(rawText);
    //        _logger.LogInformation($"📥 PARSED: {content}");
    //        if (string.IsNullOrWhiteSpace(content)) return;
    //        if (message.SenderId == _client.ClientId) return;
    //        foreach (var handler in _handlers)
    //        {
    //            _logger.LogInformation($"👉 Checking handler: {handler.GetType().Name}");

    //            if (handler.CanHandle(content))
    //            {
    //                _logger.LogInformation($"🔥 MATCHED: {handler.GetType().Name}");

    //                var response = await handler.HandleAsync(message, CancellationToken.None);

    //                if (!string.IsNullOrEmpty(response))
    //                {
    //                    await SendMessageAsync(
    //                        message.ClanId!,
    //                        message.ChannelId!,
    //                        response,
    //                        message.Mode ?? 2,
    //                        message.IsPublic ?? true
    //                    );
    //                }

    //                return;
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "❌ Handle message error");
    //    }
    //}

    private async void OnChannelMessage(object? sender, MezonEventArgs e)
    {
        try
        {
            if (_client == null) return;
            if (e.Data is not Mezon.Sdk.Domain.ChannelMessage message) return;

            //  tránh bot tự reply chính nó
            if (message.SenderId == _client.ClientId) return;
            _lastClanId = message.ClanId;
            _channelClanMap[message.ChannelId] = message.ClanId;

            _logger.LogInformation("📩 MESSAGE EVENT TRIGGERED"); 

            _logger.LogInformation($"📦 FULL: Clan={message.ClanId} Channel={message.ChannelId} Sender={message.SenderId}");

            _logger.LogInformation($"📥 RAW TEXT: {message.Content?.Text}");
            if (message.SenderId != null)
            {
                var userId = message.SenderId.ToString();

                if (!string.IsNullOrEmpty(message.Username))
                {
                    var username = message.Username.ToLower();
                    _userCache[username] = userId;
                }

                if (!string.IsNullOrEmpty(message.DisplayName))
                {
                    var display = message.DisplayName.ToLower();
                    _userCache[display] = userId;
                }

                _userCache[userId] = userId;

                var response = await _commandHandler.ProcessMessageAsync(message, _client);

                if (response == "__TEAM_FORM__")
                {
                    await _client.SendMessageAsync(
                        clanId: message.ClanId,
                        channelId: message.ChannelId,
                        mode: message.Mode ?? 2,
                        isPublic: message.IsPublic ?? true,
                        content: TaskFormBuilder.BuildTeamForm(message.ClanId!),
                        references: new[]
                        {
                            new ApiMessageRef
                            {
                                MessageId = message.MessageId,
                                MessageRefId = message.MessageId,
                                MessageSenderId = message.SenderId.ToString()
                            }
                        }
                    );
                }
                else if (!string.IsNullOrEmpty(response))
                {
                    await ReplyAsync(message, response);
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

    private async void OnButtonClicked(object? sender, MezonEventArgs e)
    {
        try
        {
            if (e.Data is not Envelope envelope || envelope.MessageButtonClicked == null)
                return;

            var data = envelope.MessageButtonClicked;
            var buttonId = data.ButtonId;

            var parts = data.ButtonId.Split('|');

            var action = parts[0];

            var channelId = data.ChannelId.ToString();

            string clanId = "";

            var messageId = data.MessageId.ToString();

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
            else
            {
                _logger.LogWarning("⚠️ fallback HARD CODE clanId");

                clanId = "2039520603727204352"; 
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
                ////  Reset form
                //await _client.Socket.UpdateChatMessageAsync(
                //    clanId: clanId,
                //    channelId: channelId,
                //    mode: ChannelStreamMode.Channel,
                //    isPublic: true,
                //    messageId: data.MessageId.ToString(),
                //    content: new ChannelMessageContent
                //    {
                //        Text = "❌ Đã huỷ nhập team",
                //        Embed = Array.Empty<object>(),
                //        Components = Array.Empty<object>()
                //    }
                //);

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
                            MessageSenderId = data.UserId.ToString()
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

                var teamName = root.GetProperty("team_name").GetString();
                var pm = root.GetProperty("pm").GetString();
                var membersRaw = root.GetProperty("members").GetString();

                if (string.IsNullOrWhiteSpace(teamName))
                {
                    await _client.SendMessageAsync(clanId, channelId, 2, true,
                        new ChannelMessageContent { Text = "❌ Team name không hợp lệ" });
                    return;
                }

                //  Parse username
                var memberUsernames = membersRaw?
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Replace("@", "").Trim().ToLower())
                    .ToList() ?? new List<string>();

                var pmUsername = pm?.Replace("@", "").Trim().ToLower() ?? "";

                //  convert PM → userId
                if (!_userCache.TryGetValue(pmUsername, out var pmUserId))
                {
                    await _client.SendMessageAsync(clanId, channelId, 2, true,
                        new ChannelMessageContent { Text = $"❌ Không tìm thấy user: {pmUsername}" });
                    return;
                }

                //  convert members → userId
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


                //  CREATE TEAM (DB)
                var team = await _teamService.CreateTeamAsync(
                    1, // projectId thật
                    teamName,
                    pmUserId,
                    memberUserIds
                );

                var clan = _client.Clans.Get(clanId);
                if (clan == null) return;

                //  gửi DM theo userId
                var allUserIds = new List<string> { pmUserId };
                allUserIds.AddRange(memberUserIds);

                foreach (var targetUserId in allUserIds.Distinct())
                {
                    var user = clan.Users.Cache
                        .FirstOrDefault(x => x.Value.Id == targetUserId)
                        .Value;

                    if (user == null) continue;

                    await user.SendDMAsync(
                        new ChannelMessageContent
                        {
                            Text = $"📩 Bạn được mời vào team `{teamName}`",
                            Components = new[]
                            {
                                new
                                {
                                    components = new object[]
                                    {
                                        new
                                        {
                                            id = $"ACCEPT|{team.Id}|{targetUserId}",
                                            type = 1,
                                            component = new { label = "Accept", style = 3 }
                                        },
                                        new
                                        {
                                            id = $"REJECT|{team.Id}|{targetUserId}",
                                            type = 1,
                                            component = new { label = "Reject", style = 4 }
                                        }
                                    }
                                }
                            }
                        }
                    );
                }

                await _client.SendMessageAsync(
                    clanId,
                    channelId,
                    2,
                    true,
                    new ChannelMessageContent
                    {
                        Text = $"✅ Team `{teamName}` đã tạo!\n👑 PM: {pm}"
                    },
                    references: new[]
                    {
                        new ApiMessageRef
                        {
                            MessageRefId = messageId,
                            MessageSenderId = data.UserId.ToString(),
                            RefType = 0
                        }
                    }
                );
            }

            if (action == "ACCEPT")
            {
                var teamId = int.Parse(parts[1]);
                var userId = parts[2];

                var member = await _context.TeamMembers
                    .FirstOrDefaultAsync(x => x.TeamId == teamId && x.Username == userId);

                if (member != null)
                {
                    member.Status = "Accepted";
                    await _context.SaveChangesAsync();
                }

                await _client.SendMessageAsync(
                    clanId,
                    channelId,
                    2,
                    true,
                    new ChannelMessageContent
                    {
                        Text = "✅ Bạn đã tham gia team!"
                    },
                    references: new[]
                    {
                        new ApiMessageRef
                        {
                            MessageRefId = messageId,
                            MessageSenderId = data.UserId.ToString(),
                            RefType = 0
                        }
                    }
                );
            }

            if (action == "REJECT")
            {
                var teamId = int.Parse(parts[1]);
                var userId = parts[2];

                var member = await _context.TeamMembers
                    .FirstOrDefaultAsync(x => x.TeamId == teamId && x.Username == userId);

                if (member != null)
                {
                    member.Status = "Rejected";
                    await _context.SaveChangesAsync();
                }
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
            //MessageId = message.MessageId,
            MessageRefId = message.MessageId,
            MessageSenderId = message.SenderId ?? "",
            //MessageSenderUsername = message.Username ?? "",
            //MessageSenderDisplayName = message.DisplayName ?? "",
            //MessageSenderClanNick = message.ClanNick ?? "",
            //MesagesSenderAvatar = message.ClanAvatar ?? "",
            //Content = message.Content?.Text ?? "",
            //HasAttachment = message.Attachments?.Any() ?? false,
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