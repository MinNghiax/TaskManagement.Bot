namespace Mezon.Sdk.Enums;

/// <summary>
/// Socket event names that can be subscribed to via <see cref="MezonClient.On(MezEvent, EventHandler)"/>.
/// </summary>
public static class MezEvent
{
    /// <summary>Listen to messages sent on a channel or thread.</summary>
    public const string ChannelMessage = "channel_message";

    /// <summary>Listen to reactions on messages.</summary>
    public const string MessageReaction = "message_reaction_event";

    /// <summary>Listen to users removed from a channel.</summary>
    public const string UserChannelRemoved = "user_channel_removed_event";

    /// <summary>Listen to users removed from a clan.</summary>
    public const string UserClanRemoved = "user_clan_removed_event";

    /// <summary>Listen to users added to a channel.</summary>
    public const string UserChannelAdded = "user_channel_added_event";

    /// <summary>Listen to channel creation events.</summary>
    public const string ChannelCreated = "channel_created_event";

    /// <summary>Listen to channel deletion events.</summary>
    public const string ChannelDeleted = "channel_deleted_event";

    /// <summary>Listen to channel update events.</summary>
    public const string ChannelUpdated = "channel_updated_event";

    /// <summary>Listen to role creation events.</summary>
    public const string RoleEvent = "role_event";

    /// <summary>Listen to "give coffee" token events.</summary>
    public const string GiveCoffee = "give_coffee_event";

    /// <summary>Listen to role assignment events.</summary>
    public const string RoleAssign = "role_assign_event";

    /// <summary>Listen to users added to a clan.</summary>
    public const string AddClanUser = "add_clan_user_event";

    /// <summary>Listen to token send events.</summary>
    public const string TokenSend = "token_sent_event";

    /// <summary>Listen to clan event creation.</summary>
    public const string ClanEventCreated = "clan_event_created";

    /// <summary>Listen to interactive button clicks on embed messages.</summary>
    public const string MessageButtonClicked = "message_button_clicked";

    /// <summary>Listen to user joining a streaming room.</summary>
    public const string StreamingJoinedEvent = "streaming_joined_event";

    /// <summary>Listen to user leaving a streaming room.</summary>
    public const string StreamingLeavedEvent = "streaming_leaved_event";

    /// <summary>Listen to dropdown selection events.</summary>
    public const string DropdownBoxSelected = "dropdown_box_selected";

    /// <summary>Listen to WebRTC signaling (1-1 call accept).</summary>
    public const string WebrtcSignalingFwd = "webrtc_signaling_fwd";

    /// <summary>Listen to voice channel start.</summary>
    public const string VoiceStartedEvent = "voice_started_event";

    /// <summary>Listen to voice channel end.</summary>
    public const string VoiceEndedEvent = "voice_ended_event";

    /// <summary>Listen to user joining a voice room.</summary>
    public const string VoiceJoinedEvent = "voice_joined_event";

    /// <summary>Listen to user leaving a voice room.</summary>
    public const string VoiceLeavedEvent = "voice_leaved_event";

    /// <summary>Listen to friend/add notification events.</summary>
    public const string Notifications = "notifications";

    /// <summary>Listen to quick menu trigger events.</summary>
    public const string QuickMenu = "quick_menu_event";
}
