using Mezon.Sdk.Domain;

namespace Mezon.Sdk.Builders;

public sealed class InteractiveBuilder
{
    private string? _color;
    private string? _title;
    private InteractiveAuthor? _author;
    private string? _description;
    private InteractiveThumbnail? _thumbnail;
    private readonly List<InteractiveField> _fields = new();
    private InteractiveImage? _image;
    private string? _timestamp;
    private InteractiveFooter? _footer;

    public InteractiveBuilder(string? title = null)
    {
        _title = title;
    }

    public InteractiveBuilder SetColor(string color)
    {
        _color = color;
        return this;
    }

    public InteractiveBuilder SetAuthor(string name, string? iconUrl = null, string? url = null)
    {
        _author = new InteractiveAuthor { Name = name, IconUrl = iconUrl, Url = url };
        return this;
    }

    public InteractiveBuilder SetDescription(string description)
    {
        _description = description;
        return this;
    }

    public InteractiveBuilder SetThumbnail(string url)
    {
        _thumbnail = new InteractiveThumbnail { Url = url };
        return this;
    }

    public InteractiveBuilder SetImage(string url, string? width = null, string? height = null)
    {
        _image = new InteractiveImage { Url = url, Width = width, Height = height };
        return this;
    }

    public InteractiveBuilder SetFooter(string text, string? iconUrl = null)
    {
        _footer = new InteractiveFooter { Text = text, IconUrl = iconUrl };
        return this;
    }

    public InteractiveBuilder SetTimestamp(string timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public InteractiveBuilder AddField(string name, string value, bool inline = false)
    {
        _fields.Add(new InteractiveField { Name = name, Value = value, Inline = inline });
        return this;
    }

    public InteractiveBuilder AddInputField(
        string id, string name,
        string? placeholder = null,
        InputFieldOption? options = null,
        string? description = null)
    {
        return this;
    }

    public InteractiveBuilder AddSelectField(
        string id, string name,
        SelectFieldOption[] options,
        SelectFieldOption? valueSelected = null,
        string? description = null)
    {
        return this;
    }

    public InteractiveBuilder AddRadioField(
        string id, string name,
        RadioFieldOption[] options,
        string? description = null,
        int? maxOptions = null)
    {
        return this;
    }

    public InteractiveBuilder AddDatePickerField(
        string id, string name, string? description = null)
    {
        return this;
    }

    public InteractiveBuilder AddAnimation(
        string id, AnimationConfig config,
        string? name = null, string? description = null)
    {
        return this;
    }

    public IInteractiveMessageProps Build() => new IInteractiveMessageProps
    {
        Color = _color,
        Title = _title,
        Url = null,
        Author = _author,
        Description = _description,
        Thumbnail = _thumbnail,
        Fields = _fields.ToArray(),
        Image = _image,
        Timestamp = _timestamp,
        Footer = _footer
    };
}