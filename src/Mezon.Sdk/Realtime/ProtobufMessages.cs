using Google.Protobuf.Collections;
using System.Text;

namespace Mezon.Sdk.Realtime;

public sealed class Envelope
{

    public string? Cid { get; set; }

    public byte[]? Channel { get; set; }
    public byte[]? ClanJoin { get; set; }
    public byte[]? ChannelJoinMsg { get; set; }
    public byte[]? ChannelLeave { get; set; }
    public byte[]? ChannelMessage { get; set; }
    public byte[]? ChannelMessageAck { get; set; }
    public byte[]? ChannelMessageSend { get; set; }
    public byte[]? ChannelMessageUpdate { get; set; }
    public byte[]? ChannelMessageRemove { get; set; }
    public byte[]? ChannelPresenceEvent { get; set; }
    public byte[]? Status { get; set; }
    public byte[]? StatusFollow { get; set; }
    public byte[]? StatusPresenceEvent { get; set; }
    public byte[]? StatusUnfollow { get; set; }
    public byte[]? StatusUpdate { get; set; }
    public byte[]? StreamData { get; set; }
    public byte[]? StreamPresenceEvent { get; set; }
    public byte[]? Ping { get; set; }
    public byte[]? Pong { get; set; }
    public byte[]? MessageTypingEvent { get; set; }
    public byte[]? LastSeenMessageEvent { get; set; }
    public byte[]? MessageReaction { get; set; }
    public byte[]? VoiceJoinedEvent { get; set; }
    public byte[]? VoiceLeavedEvent { get; set; }
    public byte[]? VoiceStartedEvent { get; set; }
    public byte[]? VoiceEndedEvent { get; set; }
    public byte[]? ChannelCreatedEvent { get; set; }
    public byte[]? ChannelDeletedEvent { get; set; }
    public byte[]? ChannelUpdatedEvent { get; set; }
    public byte[]? LastPinMessageEvent { get; set; }
    public byte[]? CustomStatusEvent { get; set; }
    public byte[]? UserChannelAddedEvent { get; set; }
    public byte[]? UserChannelRemovedEvent { get; set; }
    public byte[]? UserClanRemovedEvent { get; set; }
    public byte[]? ClanUpdatedEvent { get; set; }
    public byte[]? ClanProfileUpdatedEvent { get; set; }
    public byte[]? CheckNameExistedEvent { get; set; }
    public byte[]? UserProfileUpdatedEvent { get; set; }
    public byte[]? AddClanUserEvent { get; set; }
    public byte[]? GiveCoffeeEvent { get; set; }
    public byte[]? TokenSentEvent { get; set; }
    public byte[]? StreamingJoinedEvent { get; set; }
    public byte[]? StreamingLeavedEvent { get; set; }
    public byte[]? MessageButtonClicked { get; set; }
    public byte[]? DropdownBoxSelected { get; set; }
    public byte[]? QuickMenuDataEvent { get; set; }
    public byte[]? Notifications { get; set; }
    public byte[]? WebrtcSignalingFwd { get; set; }
    public byte[]? IncomingCallPush { get; set; }
    public byte[]? Error { get; set; }
    public byte[]? Rpc { get; set; }


    public byte[] Encode()
    {
        var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        EncodeTo(writer);
        return ms.ToArray();
    }

    public void EncodeTo(BinaryWriter writer)
    {
        if (!string.IsNullOrEmpty(Cid))
            WriteField(writer, 1, Encoding.UTF8.GetBytes(Cid));
        if (Channel != null) WriteField(writer, 2, Channel);
        if (ClanJoin != null) WriteField(writer, 3, ClanJoin);
        if (ChannelJoinMsg != null) WriteField(writer, 4, ChannelJoinMsg);
        if (ChannelLeave != null) WriteField(writer, 5, ChannelLeave);
        if (ChannelMessage != null) WriteField(writer, 6, ChannelMessage);
        if (ChannelMessageAck != null) WriteField(writer, 7, ChannelMessageAck);
        if (ChannelMessageSend != null) WriteField(writer, 8, ChannelMessageSend);
        if (ChannelMessageUpdate != null) WriteField(writer, 9, ChannelMessageUpdate);
        if (ChannelMessageRemove != null) WriteField(writer, 10, ChannelMessageRemove);
        if (ChannelPresenceEvent != null) WriteField(writer, 11, ChannelPresenceEvent);
        if (Status != null) WriteField(writer, 12, Status);
        if (StatusFollow != null) WriteField(writer, 13, StatusFollow);
        if (StatusPresenceEvent != null) WriteField(writer, 14, StatusPresenceEvent);
        if (StatusUnfollow != null) WriteField(writer, 15, StatusUnfollow);
        if (StatusUpdate != null) WriteField(writer, 16, StatusUpdate);
        if (StreamData != null) WriteField(writer, 17, StreamData);
        if (StreamPresenceEvent != null) WriteField(writer, 18, StreamPresenceEvent);
        if (Ping != null) WriteField(writer, 19, Array.Empty<byte>());
        if (Pong != null) WriteField(writer, 20, Array.Empty<byte>());
        if (MessageTypingEvent != null) WriteField(writer, 21, MessageTypingEvent);
        if (LastSeenMessageEvent != null) WriteField(writer, 22, LastSeenMessageEvent);
        if (MessageReaction != null) WriteField(writer, 23, MessageReaction);
        if (VoiceJoinedEvent != null) WriteField(writer, 24, VoiceJoinedEvent);
        if (VoiceLeavedEvent != null) WriteField(writer, 25, VoiceLeavedEvent);
        if (VoiceStartedEvent != null) WriteField(writer, 26, VoiceStartedEvent);
        if (VoiceEndedEvent != null) WriteField(writer, 27, VoiceEndedEvent);
        if (ChannelCreatedEvent != null) WriteField(writer, 28, ChannelCreatedEvent);
        if (ChannelDeletedEvent != null) WriteField(writer, 29, ChannelDeletedEvent);
        if (ChannelUpdatedEvent != null) WriteField(writer, 30, ChannelUpdatedEvent);
        if (LastPinMessageEvent != null) WriteField(writer, 31, LastPinMessageEvent);
        if (CustomStatusEvent != null) WriteField(writer, 32, CustomStatusEvent);
        if (UserChannelAddedEvent != null) WriteField(writer, 33, UserChannelAddedEvent);
        if (UserChannelRemovedEvent != null) WriteField(writer, 34, UserChannelRemovedEvent);
        if (UserClanRemovedEvent != null) WriteField(writer, 35, UserClanRemovedEvent);
        if (ClanUpdatedEvent != null) WriteField(writer, 36, ClanUpdatedEvent);
        if (ClanProfileUpdatedEvent != null) WriteField(writer, 37, ClanProfileUpdatedEvent);
        if (CheckNameExistedEvent != null) WriteField(writer, 38, CheckNameExistedEvent);
        if (UserProfileUpdatedEvent != null) WriteField(writer, 39, UserProfileUpdatedEvent);
        if (AddClanUserEvent != null) WriteField(writer, 40, AddClanUserEvent);
        if (GiveCoffeeEvent != null) WriteField(writer, 41, GiveCoffeeEvent);
        if (TokenSentEvent != null) WriteField(writer, 42, TokenSentEvent);
        if (StreamingJoinedEvent != null) WriteField(writer, 43, StreamingJoinedEvent);
        if (StreamingLeavedEvent != null) WriteField(writer, 44, StreamingLeavedEvent);
        if (MessageButtonClicked != null) WriteField(writer, 45, MessageButtonClicked);
        if (DropdownBoxSelected != null) WriteField(writer, 46, DropdownBoxSelected);
        if (QuickMenuDataEvent != null) WriteField(writer, 47, QuickMenuDataEvent);
        if (Notifications != null) WriteField(writer, 48, Notifications);
        if (WebrtcSignalingFwd != null) WriteField(writer, 49, WebrtcSignalingFwd);
        if (IncomingCallPush != null) WriteField(writer, 50, IncomingCallPush);
        if (Error != null) WriteField(writer, 51, Error);
        if (Rpc != null) WriteField(writer, 52, Rpc);
    }

    public static Envelope Decode(byte[] bytes)
    {
        var env = new Envelope();
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        DecodeFrom(reader, env);
        return env;
    }

    public static void DecodeFrom(BinaryReader reader, Envelope env)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var fieldAndWire = ReadVarint32(reader);
            var fieldNumber = fieldAndWire >> 3;
            var wireType = fieldAndWire & 0x7;

            switch (fieldNumber)
            {
                case 1:
                    env.Cid = ReadLengthDelimited(reader);
                    break;
                case 2: env.Channel = ReadLengthDelimitedBytes(reader); break;
                case 3: env.ClanJoin = ReadLengthDelimitedBytes(reader); break;
                case 4: env.ChannelJoinMsg = ReadLengthDelimitedBytes(reader); break;
                case 5: env.ChannelLeave = ReadLengthDelimitedBytes(reader); break;
                case 6: env.ChannelMessage = ReadLengthDelimitedBytes(reader); break;
                case 7: env.ChannelMessageAck = ReadLengthDelimitedBytes(reader); break;
                case 8: env.ChannelMessageSend = ReadLengthDelimitedBytes(reader); break;
                case 9: env.ChannelMessageUpdate = ReadLengthDelimitedBytes(reader); break;
                case 10: env.ChannelMessageRemove = ReadLengthDelimitedBytes(reader); break;
                case 11: env.ChannelPresenceEvent = ReadLengthDelimitedBytes(reader); break;
                case 12: env.Status = ReadLengthDelimitedBytes(reader); break;
                case 13: env.StatusFollow = ReadLengthDelimitedBytes(reader); break;
                case 14: env.StatusPresenceEvent = ReadLengthDelimitedBytes(reader); break;
                case 15: env.StatusUnfollow = ReadLengthDelimitedBytes(reader); break;
                case 16: env.StatusUpdate = ReadLengthDelimitedBytes(reader); break;
                case 17: env.StreamData = ReadLengthDelimitedBytes(reader); break;
                case 18: env.StreamPresenceEvent = ReadLengthDelimitedBytes(reader); break;
                case 19: env.Ping = Array.Empty<byte>(); break;
                case 20: env.Pong = Array.Empty<byte>(); break;
                case 21: env.MessageTypingEvent = ReadLengthDelimitedBytes(reader); break;
                case 22: env.LastSeenMessageEvent = ReadLengthDelimitedBytes(reader); break;
                case 23: env.MessageReaction = ReadLengthDelimitedBytes(reader); break;
                case 24: env.VoiceJoinedEvent = ReadLengthDelimitedBytes(reader); break;
                case 25: env.VoiceLeavedEvent = ReadLengthDelimitedBytes(reader); break;
                case 26: env.VoiceStartedEvent = ReadLengthDelimitedBytes(reader); break;
                case 27: env.VoiceEndedEvent = ReadLengthDelimitedBytes(reader); break;
                case 28: env.ChannelCreatedEvent = ReadLengthDelimitedBytes(reader); break;
                case 29: env.ChannelDeletedEvent = ReadLengthDelimitedBytes(reader); break;
                case 30: env.ChannelUpdatedEvent = ReadLengthDelimitedBytes(reader); break;
                case 31: env.LastPinMessageEvent = ReadLengthDelimitedBytes(reader); break;
                case 32: env.CustomStatusEvent = ReadLengthDelimitedBytes(reader); break;
                case 33: env.UserChannelAddedEvent = ReadLengthDelimitedBytes(reader); break;
                case 34: env.UserChannelRemovedEvent = ReadLengthDelimitedBytes(reader); break;
                case 35: env.UserClanRemovedEvent = ReadLengthDelimitedBytes(reader); break;
                case 36: env.ClanUpdatedEvent = ReadLengthDelimitedBytes(reader); break;
                case 37: env.ClanProfileUpdatedEvent = ReadLengthDelimitedBytes(reader); break;
                case 38: env.CheckNameExistedEvent = ReadLengthDelimitedBytes(reader); break;
                case 39: env.UserProfileUpdatedEvent = ReadLengthDelimitedBytes(reader); break;
                case 40: env.AddClanUserEvent = ReadLengthDelimitedBytes(reader); break;
                case 41: env.GiveCoffeeEvent = ReadLengthDelimitedBytes(reader); break;
                case 42: env.TokenSentEvent = ReadLengthDelimitedBytes(reader); break;
                case 43: env.StreamingJoinedEvent = ReadLengthDelimitedBytes(reader); break;
                case 44: env.StreamingLeavedEvent = ReadLengthDelimitedBytes(reader); break;
                case 45: env.MessageButtonClicked = ReadLengthDelimitedBytes(reader); break;
                case 46: env.DropdownBoxSelected = ReadLengthDelimitedBytes(reader); break;
                case 47: env.QuickMenuDataEvent = ReadLengthDelimitedBytes(reader); break;
                case 48: env.Notifications = ReadLengthDelimitedBytes(reader); break;
                case 49: env.WebrtcSignalingFwd = ReadLengthDelimitedBytes(reader); break;
                case 50: env.IncomingCallPush = ReadLengthDelimitedBytes(reader); break;
                case 51: env.Error = ReadLengthDelimitedBytes(reader); break;
                case 52: env.Rpc = ReadLengthDelimitedBytes(reader); break;
                default:
                    if (wireType == 0) ReadVarint64(reader);
                    else if (wireType == 2) { var len = ReadVarint32(reader); reader.BaseStream.Position += len; }
                    else if (wireType == 1) reader.BaseStream.Position += 8;
                    else if (wireType == 5) reader.BaseStream.Position += 4;
                    break;
            }
        }
    }


    private static void WriteField(BinaryWriter writer, int fieldNumber, byte[] data)
    {
        var tag = (fieldNumber << 3) | 2; 
        WriteVarint(writer, tag);
        WriteVarint(writer, data.Length);
        writer.Write(data);
    }

    private static void WriteVarint(BinaryWriter writer, int value)
    {
        var v = (uint)value;
        while (v > 0x7F)
        {
            writer.Write((byte)((v & 0x7F) | 0x80));
            v >>= 7;
        }
        writer.Write((byte)(v & 0x7F));
    }

    private static int ReadVarint32(BinaryReader reader)
    {
        int result = 0;
        int shift = 0;
        while (true)
        {
            var b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
            if (shift >= 32) throw new InvalidDataException("Varint too long");
        }
        return result;
    }

    private static long ReadVarint64(BinaryReader reader)
    {
        long result = 0;
        int shift = 0;
        while (true)
        {
            var b = reader.ReadByte();
            result |= (long)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
            if (shift >= 64) throw new InvalidDataException("Varint too long");
        }
        return result;
    }

    private static byte[] ReadLengthDelimitedBytes(BinaryReader reader)
    {
        var len = ReadVarint32(reader);
        return reader.ReadBytes(len);
    }

    private static string ReadLengthDelimited(BinaryReader reader)
    {
        var len = ReadVarint32(reader);
        var bytes = reader.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }

    public override string ToString()
    {
        var fields = new List<string>();
        if (Cid != null) fields.Add($"Cid={Cid}");
        if (Ping != null) fields.Add("Ping");
        if (Pong != null) fields.Add("Pong");
        if (Channel != null) fields.Add("Channel");
        if (ChannelMessage != null) fields.Add("ChannelMessage");
        if (ChannelMessageAck != null) fields.Add("ChannelMessageAck");
        if (ChannelMessageSend != null) fields.Add("ChannelMessageSend");
        if (TokenSentEvent != null) fields.Add("TokenSentEvent");
        return $"Envelope[{string.Join(", ", fields)}]";
    }
}


public static class RealtimeSerializer
{
    public static byte[] Encode(Envelope envelope) => envelope.Encode();

    public static Envelope Decode(byte[] bytes) => Envelope.Decode(bytes);
}


public sealed class Ping
{
    public static Ping Default { get; } = new();
    public byte[] Encode() => Array.Empty<byte>();
}

public sealed class Pong
{
    public static Pong Default { get; } = new();
}


public sealed class UserPresence
{
    public string UserId { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string Username { get; set; } = "";
    public bool IsMobile { get; set; }
    public string UserStatus { get; set; } = "";

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, UserId);
        WriteString(writer, 2, SessionId);
        WriteString(writer, 3, Username);
        if (IsMobile) WriteVarint(writer, 4, 1);
        WriteString(writer, 5, UserStatus);
        return ms.ToArray();
    }

    public static UserPresence Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var p = new UserPresence();
        DecodeFrom(reader, p);
        return p;
    }

    private static void DecodeFrom(BinaryReader reader, UserPresence p)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            if ((tag & 0x7) == 2)
            {
                switch (field)
                {
                    case 1: p.UserId = ReadLengthDelimitedStr(reader); break;
                    case 2: p.SessionId = ReadLengthDelimitedStr(reader); break;
                    case 3: p.Username = ReadLengthDelimitedStr(reader); break;
                    case 5: p.UserStatus = ReadLengthDelimitedStr(reader); break;
                    default: SkipField(reader); break;
                }
            }
            else if ((tag & 0x7) == 0)
            {
                if (field == 4) p.IsMobile = ReadVarint32(reader) != 0;
                else SkipVarint(reader);
            }
            else SkipField(reader);
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, (uint)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarint(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, (uint)value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType)
    {
        WriteVarint(writer, (uint)((field << 3) | wireType));
    }

    private static void WriteVarint(BinaryWriter writer, uint value)
    {
        while (value > 0x7F) { writer.Write((byte)((value & 0x7F) | 0x80)); value >>= 7; }
        writer.Write((byte)(value & 0x7F));
    }

    private static int ReadVarint32(BinaryReader reader)
    {
        int result = 0, shift = 0;
        while (true)
        {
            var b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }
        return result;
    }

    private static string ReadLengthDelimitedStr(BinaryReader reader)
    {
        var len = ReadVarint32(reader);
        return Encoding.UTF8.GetString(reader.ReadBytes(len));
    }

    private static void SkipField(BinaryReader reader)
    {
        var b = reader.ReadByte();
        var wireType = b & 0x7;
        if (wireType == 0) { while ((reader.PeekChar() & 0x80) != 0) reader.ReadByte(); reader.ReadByte(); }
        else if (wireType == 2) { var len = ReadVarint32(reader); reader.BaseStream.Position += len; }
        else if (wireType == 1) reader.BaseStream.Position += 8;
        else if (wireType == 5) reader.BaseStream.Position += 4;
    }

    private static void SkipVarint(BinaryReader reader)
    {
        while ((reader.PeekChar() & 0x80) != 0) reader.ReadByte();
        reader.ReadByte();
    }
}


public sealed class Channel
{
    public string Id { get; set; } = "";
    public RepeatedField<UserPresence> Presences { get; } = new();
    public UserPresence? Self { get; set; }
    public string ChanelLabel { get; set; } = "";
    public string ClanLogo { get; set; } = "";
    public string CategoryName { get; set; } = "";

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, Id);
        foreach (var p in Presences) WriteBytes(writer, 2, p.Encode());
        if (Self != null) WriteBytes(writer, 3, Self.Encode());
        WriteString(writer, 4, ChanelLabel);
        WriteString(writer, 5, ClanLogo);
        WriteString(writer, 6, CategoryName);
        return ms.ToArray();
    }

    public static Channel Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var c = new Channel();
        DecodeFrom(reader, c);
        return c;
    }

    private static void DecodeFrom(BinaryReader reader, Channel c)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var fieldData = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: c.Id = Encoding.UTF8.GetString(fieldData); break;
                    case 2: c.Presences.Add(UserPresence.Decode(fieldData)); break;
                    case 3: c.Self = UserPresence.Decode(fieldData); break;
                    case 4: c.ChanelLabel = Encoding.UTF8.GetString(fieldData); break;
                    case 5: c.ClanLogo = Encoding.UTF8.GetString(fieldData); break;
                    case 6: c.CategoryName = Encoding.UTF8.GetString(fieldData); break;
                }
            }
            else SkipField(reader);
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteBytes(BinaryWriter writer, int field, byte[] data)
    {
        WriteTag(writer, field, 2);
        WriteVarint(writer, data.Length);
        writer.Write(data);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType)
    {
        WriteVarint(writer, (field << 3) | wireType);
    }

    private static void WriteVarint(BinaryWriter writer, int value)
    {
        uint v = (uint)value;
        while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; }
        writer.Write((byte)(v & 0x7F));
    }

    private static int ReadVarint32(BinaryReader reader)
    {
        int result = 0, shift = 0;
        while (true)
        {
            var b = reader.ReadByte();
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) break;
            shift += 7;
        }
        return result;
    }

    private static void SkipField(BinaryReader reader)
    {
        var b = reader.PeekChar();
        var wireType = b & 0x7;
        if (wireType == 0) { while ((reader.ReadByte() & 0x80) != 0) { } }
        else if (wireType == 2) { var len = ReadVarint32(reader); reader.BaseStream.Position += len; }
        else if (wireType == 1) reader.BaseStream.Position += 8;
        else if (wireType == 5) reader.BaseStream.Position += 4;
    }
}


public sealed class ClanJoin
{
    public string ClanId { get; set; } = "";
    public byte[] Encode()
    {
        if (string.IsNullOrEmpty(ClanId)) return Array.Empty<byte>();
        var bytes = Encoding.UTF8.GetBytes(ClanId);
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteTag(writer, 1, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
        return ms.ToArray();
    }
    public static ClanJoin Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var j = new ClanJoin();
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2 && field == 1)
            {
                var len = ReadVarint32(reader);
                j.ClanId = Encoding.UTF8.GetString(reader.ReadBytes(len));
            }
            else break;
        }
        return j;
    }
    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}


public sealed class ChannelJoin
{
    public string ClanId { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public int ChannelType { get; set; }
    public bool IsPublic { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ClanId);
        WriteString(writer, 2, ChannelId);
        if (ChannelType != 0) WriteVarintField(writer, 3, ChannelType);
        if (IsPublic) WriteVarintField(writer, 4, 1);
        return ms.ToArray();
    }

    public static ChannelJoin Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var j = new ChannelJoin();
        DecodeFrom(reader, j);
        return j;
    }

    private static void DecodeFrom(BinaryReader reader, ChannelJoin j)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                if (field == 1) j.ClanId = Encoding.UTF8.GetString(data);
                else if (field == 2) j.ChannelId = Encoding.UTF8.GetString(data);
                else break;
            }
            else if (wireType == 0)
            {
                var v = ReadVarint32(reader);
                if (field == 3) j.ChannelType = v;
                else if (field == 4) j.IsPublic = v != 0;
                else break;
            }
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}

public sealed class ChannelLeave
{
    public string ClanId { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public int ChannelType { get; set; }
    public bool IsPublic { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ClanId);
        WriteString(writer, 2, ChannelId);
        if (ChannelType != 0) WriteVarintField(writer, 3, ChannelType);
        if (IsPublic) WriteVarintField(writer, 4, 1);
        return ms.ToArray();
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
}


public sealed class ChannelMessageSend
{
    public string ClanId { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public string Content { get; set; } = "";
    public RepeatedField<MessageMention> Mentions { get; } = new();
    public RepeatedField<MessageAttachment> Attachments { get; } = new();
    public RepeatedField<MessageRef> References { get; } = new();
    public int Mode { get; set; }
    public bool AnonymousMessage { get; set; }
    public bool MentionEveryone { get; set; }
    public string Avatar { get; set; } = "";
    public bool IsPublic { get; set; }
    public int Code { get; set; }
    public string TopicId { get; set; } = "";
    public string Id { get; set; } = "";

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ClanId);
        WriteString(writer, 2, ChannelId);
        WriteString(writer, 3, Content);
        foreach (var m in Mentions) WriteBytes(writer, 4, m.Encode());
        foreach (var a in Attachments) WriteBytes(writer, 5, a.Encode());
        foreach (var r in References) WriteBytes(writer, 6, r.Encode());
        if (Mode != 0) WriteVarintField(writer, 7, Mode);
        if (AnonymousMessage) WriteVarintField(writer, 8, 1);
        if (MentionEveryone) WriteVarintField(writer, 9, 1);
        WriteString(writer, 10, Avatar);
        if (IsPublic) WriteVarintField(writer, 11, 1);
        if (Code != 0) WriteVarintField(writer, 12, Code);
        WriteString(writer, 13, TopicId);
        WriteString(writer, 14, Id);
        return ms.ToArray();
    }

    public static ChannelMessageSend Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var m = new ChannelMessageSend();
        DecodeFrom(reader, m);
        return m;
    }

    private static void DecodeFrom(BinaryReader reader, ChannelMessageSend m)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: m.ClanId = Encoding.UTF8.GetString(data); break;
                    case 2: m.ChannelId = Encoding.UTF8.GetString(data); break;
                    case 3: m.Content = Encoding.UTF8.GetString(data); break;
                    case 4: m.Mentions.Add(MessageMention.Decode(data)); break;
                    case 5: m.Attachments.Add(MessageAttachment.Decode(data)); break;
                    case 6: m.References.Add(MessageRef.Decode(data)); break;
                    case 10: m.Avatar = Encoding.UTF8.GetString(data); break;
                    case 13: m.TopicId = Encoding.UTF8.GetString(data); break;
                    case 14: m.Id = Encoding.UTF8.GetString(data); break;
                }
            }
            else if (wireType == 0)
            {
                var v = ReadVarint32(reader);
                switch (field)
                {
                    case 7: m.Mode = v; break;
                    case 8: m.AnonymousMessage = v != 0; break;
                    case 9: m.MentionEveryone = v != 0; break;
                    case 11: m.IsPublic = v != 0; break;
                    case 12: m.Code = v; break;
                }
            }
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteBytes(BinaryWriter writer, int field, byte[] data)
    {
        WriteTag(writer, field, 2);
        WriteVarint(writer, data.Length);
        writer.Write(data);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}


public sealed class MessageMention
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    public string RoleId { get; set; } = "";
    public string RoleName { get; set; } = "";
    public long CreateTimeSeconds { get; set; }
    public int S { get; set; }
    public int E { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, Id);
        WriteString(writer, 2, UserId);
        WriteString(writer, 3, Username);
        WriteString(writer, 4, RoleId);
        WriteString(writer, 5, RoleName);
        if (CreateTimeSeconds != 0) WriteFixed64(writer, 6, (ulong)CreateTimeSeconds);
        if (S != 0) WriteVarintField(writer, 7, S);
        if (E != 0) WriteVarintField(writer, 8, E);
        return ms.ToArray();
    }

    public static MessageMention Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var m = new MessageMention();
        DecodeFrom(reader, m);
        return m;
    }

    private static void DecodeFrom(BinaryReader reader, MessageMention m)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: m.Id = Encoding.UTF8.GetString(data); break;
                    case 2: m.UserId = Encoding.UTF8.GetString(data); break;
                    case 3: m.Username = Encoding.UTF8.GetString(data); break;
                    case 4: m.RoleId = Encoding.UTF8.GetString(data); break;
                    case 5: m.RoleName = Encoding.UTF8.GetString(data); break;
                }
            }
            else if (wireType == 0)
            {
                var v = ReadVarint32(reader);
                if (field == 7) m.S = v;
                else if (field == 8) m.E = v;
            }
            else if (wireType == 1 && field == 6) m.CreateTimeSeconds = (long)ReadFixed64(reader);
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteFixed64(BinaryWriter writer, int field, ulong value)
    {
        WriteTag(writer, field, 1);
        writer.Write(value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
    private static ulong ReadFixed64(BinaryReader reader) => reader.ReadUInt64();
}

public sealed class MessageAttachment
{
    public string Filename { get; set; } = "";
    public int Size { get; set; }
    public string Url { get; set; } = "";
    public string FileType { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, Filename);
        if (Size != 0) WriteVarintField(writer, 2, Size);
        WriteString(writer, 3, Url);
        WriteString(writer, 4, FileType);
        if (Width != 0) WriteVarintField(writer, 5, Width);
        if (Height != 0) WriteVarintField(writer, 6, Height);
        return ms.ToArray();
    }

    public static MessageAttachment Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var a = new MessageAttachment();
        DecodeFrom(reader, a);
        return a;
    }

    private static void DecodeFrom(BinaryReader reader, MessageAttachment a)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: a.Filename = Encoding.UTF8.GetString(data); break;
                    case 3: a.Url = Encoding.UTF8.GetString(data); break;
                    case 4: a.FileType = Encoding.UTF8.GetString(data); break;
                }
            }
            else if (wireType == 0)
            {
                var v = ReadVarint32(reader);
                if (field == 2) a.Size = v;
                else if (field == 5) a.Width = v;
                else if (field == 6) a.Height = v;
            }
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}

public sealed class MessageRef
{
    public string MessageId { get; set; } = "";
    public string MessageRefId { get; set; } = "";
    public string Content { get; set; } = "";
    public bool HasAttachment { get; set; }
    public int RefType { get; set; }
    public string MessageSenderId { get; set; } = "";
    public string MessageSenderUsername { get; set; } = "";

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, MessageId);
        WriteString(writer, 2, MessageRefId);
        WriteString(writer, 3, Content);
        if (HasAttachment) WriteVarintField(writer, 4, 1);
        if (RefType != 0) WriteVarintField(writer, 5, RefType);
        WriteString(writer, 6, MessageSenderId);
        WriteString(writer, 7, MessageSenderUsername);
        return ms.ToArray();
    }

    public static MessageRef Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var r = new MessageRef();
        DecodeFrom(reader, r);
        return r;
    }

    private static void DecodeFrom(BinaryReader reader, MessageRef r)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: r.MessageId = Encoding.UTF8.GetString(data); break;
                    case 2: r.MessageRefId = Encoding.UTF8.GetString(data); break;
                    case 3: r.Content = Encoding.UTF8.GetString(data); break;
                    case 6: r.MessageSenderId = Encoding.UTF8.GetString(data); break;
                    case 7: r.MessageSenderUsername = Encoding.UTF8.GetString(data); break;
                }
            }
            else if (wireType == 0)
            {
                var v = ReadVarint32(reader);
                if (field == 4) r.HasAttachment = v != 0;
                else if (field == 5) r.RefType = v;
            }
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}


public sealed class RealtimeChannelMessageAck
{
    public string ChannelId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public int Code { get; set; }
    public string Username { get; set; } = "";
    public long CreateTimeSeconds { get; set; }
    public long UpdateTimeSeconds { get; set; }
    public bool Persistent { get; set; }
    public string ClanLogo { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int Mode { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ChannelId);
        WriteString(writer, 2, MessageId);
        if (Code != 0) WriteVarintField(writer, 3, Code);
        WriteString(writer, 4, Username);
        if (CreateTimeSeconds != 0) WriteFixed64(writer, 5, (ulong)CreateTimeSeconds);
        if (UpdateTimeSeconds != 0) WriteFixed64(writer, 6, (ulong)UpdateTimeSeconds);
        if (Persistent) WriteVarintField(writer, 7, 1);
        WriteString(writer, 8, ClanLogo);
        WriteString(writer, 9, CategoryName);
        if (Mode != 0) WriteVarintField(writer, 10, Mode);
        return ms.ToArray();
    }

    public static RealtimeChannelMessageAck Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var a = new RealtimeChannelMessageAck();
        DecodeFrom(reader, a);
        return a;
    }

    private static void DecodeFrom(BinaryReader reader, RealtimeChannelMessageAck a)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: a.ChannelId = Encoding.UTF8.GetString(data); break;
                    case 2: a.MessageId = Encoding.UTF8.GetString(data); break;
                    case 4: a.Username = Encoding.UTF8.GetString(data); break;
                    case 8: a.ClanLogo = Encoding.UTF8.GetString(data); break;
                    case 9: a.CategoryName = Encoding.UTF8.GetString(data); break;
                }
            }
            else if (wireType == 0)
            {
                var v = ReadVarint32(reader);
                if (field == 3) a.Code = v;
                else if (field == 7) a.Persistent = v != 0;
                else if (field == 10) a.Mode = v;
            }
            else if (wireType == 1)
            {
                if (field == 5) a.CreateTimeSeconds = (long)reader.ReadUInt64();
                else if (field == 6) a.UpdateTimeSeconds = (long)reader.ReadUInt64();
                else reader.ReadUInt64();
            }
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteFixed64(BinaryWriter writer, int field, ulong value)
    {
        WriteTag(writer, field, 1);
        writer.Write(value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}


public sealed class ChannelMessageUpdate
{
    public string ClanId { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string Content { get; set; } = "";
    public bool Hidden { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ClanId);
        WriteString(writer, 2, ChannelId);
        WriteString(writer, 3, MessageId);
        WriteString(writer, 4, Content);
        if (Hidden) WriteVarintField(writer, 5, 1);
        return ms.ToArray();
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
}

public sealed class ChannelMessageRemove
{
    public string ClanId { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public string Deletor { get; set; } = "";

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ClanId);
        WriteString(writer, 2, ChannelId);
        WriteString(writer, 3, MessageId);
        WriteString(writer, 4, Deletor);
        return ms.ToArray();
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
}


public sealed class RealtimeChannelMessage
{
    public string ClanId { get; set; } = "";
    public string ChannelId { get; set; } = "";
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public RepeatedField<MessageMention> Mentions { get; } = new();
    public RepeatedField<MessageAttachment> Attachments { get; } = new();
    public RepeatedField<MessageRef> References { get; } = new();
    public string SenderId { get; set; } = "";
    public string Username { get; set; } = "";
    public string ClanLogo { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string CreateTime { get; set; } = "";
    public string UpdateTime { get; set; } = "";
    public long CreateTimeSeconds { get; set; }
    public long UpdateTimeSeconds { get; set; }
    public int Mode { get; set; }
    public string TopicId { get; set; } = "";
    public bool IsPublic { get; set; }
    public string MessageId { get; set; } = "";

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, ClanId);
        WriteString(writer, 2, ChannelId);
        WriteString(writer, 3, Id);
        WriteString(writer, 4, Content);
        foreach (var m in Mentions) WriteBytes(writer, 5, m.Encode());
        foreach (var a in Attachments) WriteBytes(writer, 6, a.Encode());
        foreach (var r in References) WriteBytes(writer, 7, r.Encode());
        WriteString(writer, 8, SenderId);
        WriteString(writer, 9, Username);
        WriteString(writer, 10, ClanLogo);
        WriteString(writer, 11, CategoryName);
        WriteString(writer, 12, CreateTime);
        WriteString(writer, 13, UpdateTime);
        if (CreateTimeSeconds != 0) WriteVarintField(writer, 14, (int)CreateTimeSeconds);
        if (UpdateTimeSeconds != 0) WriteVarintField(writer, 15, (int)UpdateTimeSeconds);
        if (Mode != 0) WriteVarintField(writer, 16, Mode);
        WriteString(writer, 17, TopicId);
        if (IsPublic) WriteVarintField(writer, 18, 1);
        WriteString(writer, 19, MessageId);
        return ms.ToArray();
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteBytes(BinaryWriter writer, int field, byte[] data)
    {
        WriteTag(writer, field, 2);
        WriteVarint(writer, data.Length);
        writer.Write(data);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
}


public sealed class Status
{
    public RepeatedField<UserPresence> Presences { get; } = new();
    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        foreach (var p in Presences) { WriteBytes(writer, 1, p.Encode()); }
        return ms.ToArray();
    }
    private static void WriteBytes(BinaryWriter writer, int field, byte[] data) { WriteTag(writer, field, 2); WriteVarint(writer, data.Length); writer.Write(data); }
    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
}

public sealed class StatusFollow { public RepeatedField<string> UserIds { get; } = new(); public RepeatedField<string> Usernames { get; } = new(); }
public sealed class StatusPresenceEvent { public RepeatedField<UserPresence> Joins { get; } = new(); public RepeatedField<UserPresence> Leaves { get; } = new(); }
public sealed class StatusUnfollow { public RepeatedField<string> UserIds { get; } = new(); }
public sealed class StatusUpdate { public string Status { get; set; } = ""; }
public sealed class StreamData { }
public sealed class StreamPresenceEvent { }
public sealed class ChannelPresenceEvent { }
public sealed class Notifications { }
public sealed class MessageTypingEvent { }
public sealed class LastSeenMessageEvent { }
public sealed class MessageReaction { }
public sealed class VoiceJoinedEvent { }
public sealed class VoiceLeavedEvent { }
public sealed class VoiceStartedEvent { }
public sealed class VoiceEndedEvent { }
public sealed class ChannelCreatedEvent { }
public sealed class ChannelDeletedEvent { }
public sealed class ChannelUpdatedEvent { }
public sealed class LastPinMessageEvent { }
public sealed class CustomStatusEvent { }
public sealed class UserChannelAddedEvent { }
public sealed class UserChannelRemovedEvent { }
public sealed class UserClanRemovedEvent { }
public sealed class ClanUpdatedEvent { }
public sealed class ClanProfileUpdatedEvent { }
public sealed class CheckNameExistedEvent { }
public sealed class UserProfileUpdatedEvent { }
public sealed class AddClanUserEvent { }
public sealed class GiveCoffeeEvent { }
public sealed class StreamingJoinedEvent { }
public sealed class StreamingLeavedEvent { }
public sealed class MessageButtonClicked { }
public sealed class DropdownBoxSelected { }
public sealed class QuickMenuDataEvent { }
public sealed class WebrtcSignalingFwd { }
public sealed class IncomingCallPush { }
public sealed class Error { }
public sealed class Rpc { }


public sealed class TokenSentEvent
{
    public string SenderId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string ReceiverId { get; set; } = "";
    public int Amount { get; set; }
    public string? Note { get; set; }
    public string? ExtraAttribute { get; set; }
    public string? TransactionId { get; set; }

    public byte[] Encode()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);
        WriteString(writer, 1, SenderId);
        WriteString(writer, 2, SenderName);
        WriteString(writer, 3, ReceiverId);
        if (Amount != 0) WriteVarintField(writer, 4, Amount);
        return ms.ToArray();
    }

    public static TokenSentEvent Decode(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        using var reader = new BinaryReader(ms, Encoding.UTF8);
        var e = new TokenSentEvent();
        DecodeFrom(reader, e);
        return e;
    }

    private static void DecodeFrom(BinaryReader reader, TokenSentEvent e)
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var tag = ReadVarint32(reader);
            var field = tag >> 3;
            var wireType = tag & 0x7;
            if (wireType == 2)
            {
                var len = ReadVarint32(reader);
                var data = reader.ReadBytes(len);
                switch (field)
                {
                    case 1: e.SenderId = Encoding.UTF8.GetString(data); break;
                    case 2: e.SenderName = Encoding.UTF8.GetString(data); break;
                    case 3: e.ReceiverId = Encoding.UTF8.GetString(data); break;
                }
            }
            else if (wireType == 0 && field == 4) e.Amount = ReadVarint32(reader);
            else break;
        }
    }

    private static void WriteString(BinaryWriter writer, int field, string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteTag(writer, field, 2);
        WriteVarint(writer, bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteVarintField(BinaryWriter writer, int field, int value)
    {
        if (value == 0) return;
        WriteTag(writer, field, 0);
        WriteVarint(writer, value);
    }

    private static void WriteTag(BinaryWriter writer, int field, int wireType) => WriteVarint(writer, (field << 3) | wireType);
    private static void WriteVarint(BinaryWriter writer, int value) { uint v = (uint)value; while (v > 0x7F) { writer.Write((byte)((v & 0x7F) | 0x80)); v >>= 7; } writer.Write((byte)(v & 0x7F)); }
    private static int ReadVarint32(BinaryReader reader) { int result = 0, shift = 0; while (true) { var b = reader.ReadByte(); result |= (b & 0x7F) << shift; if ((b & 0x80) == 0) break; shift += 7; } return result; }
}
