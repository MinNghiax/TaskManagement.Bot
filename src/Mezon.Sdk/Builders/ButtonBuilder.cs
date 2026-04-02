namespace Mezon.Sdk.Builders;

using System.Text.Json;
using System.Text.Json.Serialization;

public enum ButtonStyle { Default = 1, Primary = 2, Success = 3, Danger = 4, Link = 5 }

public class ButtonComponent
{
    [JsonPropertyName("id")]    public string Id    { get; set; } = "";
    [JsonPropertyName("type")]  public int Type     { get; set; } = 2; // BUTTON
    [JsonPropertyName("component")] public ButtonInner Component { get; set; } = new();
}

public class ButtonInner
{
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("style")] public int Style    { get; set; } = (int)ButtonStyle.Default;
}

public class ButtonBuilder
{
    private readonly List<ButtonComponent> _components = [];

    public ButtonBuilder AddButton(string id, string label, ButtonStyle style = ButtonStyle.Default)
    {
        _components.Add(new ButtonComponent
        {
            Id = id,
            Component = new ButtonInner { Label = label, Style = (int)style },
        });
        return this;
    }

    public List<ButtonComponent> Build() => _components;

    public string ToJson() => JsonSerializer.Serialize(_components);
}
