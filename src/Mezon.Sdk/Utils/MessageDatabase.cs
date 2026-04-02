namespace Mezon.Sdk.Utils;

using Microsoft.Data.Sqlite;

public class MessageDatabase : IDisposable
{
    private readonly SqliteConnection _db;

    public MessageDatabase(string path = "messages.db")
    {
        _db = new SqliteConnection($"Data Source={path}");
        _db.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS messages (
            id TEXT PRIMARY KEY,
            clan_id TEXT,
            channel_id TEXT,
            sender_id TEXT,
            content TEXT,
            create_time INTEGER
        )";
        cmd.ExecuteNonQuery();
    }

    public void SaveMessage(string id, string? clanId, string? channelId, string? senderId, string? content, long createTime = 0)
    {
        using var cmd = _db.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO messages(id, clan_id, channel_id, sender_id, content, create_time) VALUES($id,$clan,$ch,$sender,$content,$time)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$clan", (object?)clanId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ch", (object?)channelId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sender", (object?)senderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$content", (object?)content ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$time", createTime);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() { _db.Dispose(); }
}
