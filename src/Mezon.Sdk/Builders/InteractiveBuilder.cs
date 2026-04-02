namespace Mezon.Sdk.Builders;

using System.Text.Json;
using System.Text.Json.Serialization;

public class InteractiveField
{
    [JsonPropertyName("name")]   public string Name  { get; set; } = "";
    [JsonPropertyName("value")]  public string Value { get; set; } = "";
    [JsonPropertyName("inline")] public bool Inline  { get; set; }
    [JsonPropertyName("inputs"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Inputs { get; set; }
}

public class InteractiveAuthor
{
    [JsonPropertyName("name")]     public string Name    { get; set; } = "";
    [JsonPropertyName("icon_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IconUrl { get; set; }
    [JsonPropertyName("url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }
}

public class InteractiveFooter
{
    [JsonPropertyName("text")]     public string Text    { get; set; } = "";
    [JsonPropertyName("icon_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IconUrl { get; set; }
}

public class InteractiveImage
{
    [JsonPropertyName("url")]    public string Url    { get; set; } = "";
    [JsonPropertyName("width")]  public string Width  { get; set; } = "auto";
    [JsonPropertyName("height")] public string Height { get; set; } = "auto";
}

public class InteractiveMessage
{
    [JsonPropertyName("color")]       public string Color     { get; set; } = "#5865F2";
    [JsonPropertyName("title"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
    [JsonPropertyName("description"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    [JsonPropertyName("author"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveAuthor? Author { get; set; }
    [JsonPropertyName("thumbnail"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveImage? Thumbnail { get; set; }
    [JsonPropertyName("image"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveImage? Image { get; set; }
    [JsonPropertyName("fields")]  public List<InteractiveField> Fields { get; set; } = [];
    [JsonPropertyName("footer"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InteractiveFooter? Footer { get; set; }
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
}

public class InteractiveBuilder
{
    private readonly InteractiveMessage _msg;

    public InteractiveBuilder(string? title = null)
    {
        _msg = new InteractiveMessage
        {
            Title = title,
            Footer = new InteractiveFooter { Text = "Powered by Mezon" },
        };
    }

    public InteractiveBuilder SetColor(string hex)          { _msg.Color = hex; return this; }
    public InteractiveBuilder SetDescription(string desc)   { _msg.Description = desc; return this; }
    public InteractiveBuilder SetThumbnail(string url)      { _msg.Thumbnail = new InteractiveImage { Url = url }; return this; }
    public InteractiveBuilder SetImage(string url, string width = "auto", string height = "auto")
    {
        _msg.Image = new InteractiveImage { Url = url, Width = width, Height = height };
        return this;
    }

    public InteractiveBuilder SetAuthor(string name, string? iconUrl = null, string? url = null)
    {
        _msg.Author = new InteractiveAuthor { Name = name, IconUrl = iconUrl, Url = url };
        return this;
    }

    public InteractiveBuilder AddField(string name, string value, bool inline = false)
    {
        _msg.Fields.Add(new InteractiveField { Name = name, Value = value, Inline = inline });
        return this;
    }

    public InteractiveMessage Build() => _msg;

    public string ToJson() => JsonSerializer.Serialize(_msg, new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    });
}
