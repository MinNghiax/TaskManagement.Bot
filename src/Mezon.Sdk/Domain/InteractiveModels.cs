namespace Mezon.Sdk.Domain;

public sealed record IInteractiveMessageProps
{
    public string? Color { get; init; }
    public string? Title { get; init; }
    public string? Url { get; init; }
    public InteractiveAuthor? Author { get; init; }
    public string? Description { get; init; }
    public InteractiveThumbnail? Thumbnail { get; init; }
    public InteractiveField[]? Fields { get; init; }
    public InteractiveImage? Image { get; init; }
    public string? Timestamp { get; init; }
    public InteractiveFooter? Footer { get; init; }
}

public sealed record InteractiveAuthor
{
    public required string Name { get; init; }
    public string? IconUrl { get; init; }
    public string? Url { get; init; }
}

public sealed record InteractiveThumbnail
{
    public required string Url { get; init; }
}

public sealed record InteractiveField
{
    public required string Name { get; init; }
    public required string Value { get; init; }
    public bool? Inline { get; init; }
}

public sealed record InteractiveImage
{
    public required string Url { get; init; }
    public string? Width { get; init; }
    public string? Height { get; init; }
}

public sealed record InteractiveFooter
{
    public required string Text { get; init; }
    public string? IconUrl { get; init; }
}

public sealed record InputFieldOption
{
    public object? DefaultValue { get; init; }
    public string? Type { get; init; }
    public bool? Textarea { get; init; }
    public bool? Disabled { get; init; }
}

public sealed record SelectFieldOption
{
    public required string Label { get; init; }
    public required string Value { get; init; }
}

public sealed record RadioFieldOption
{
    public required string Label { get; init; }
    public required string Value { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Style { get; init; }
    public bool? Disabled { get; init; }
}

public sealed record AnimationConfig
{
    public required string UrlImage { get; init; }
    public required string UrlPosition { get; init; }
    public required string[] Pool { get; init; }
    public int? Repeat { get; init; }
    public int? Duration { get; init; }
}

public sealed record IMessageComponent
{
    public required int Type { get; init; }
    public required string Id { get; init; }
    public object? Component { get; init; }
}

public sealed record IButtonMessage
{
    public required string Label { get; init; }
    public bool? Disable { get; init; }
    public int? Style { get; init; }
    public string? Url { get; init; }
}

public sealed record IMessageActionRow
{
    public required IMessageComponent[] Components { get; init; }
}
