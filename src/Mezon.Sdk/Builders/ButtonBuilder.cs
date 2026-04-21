using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;

namespace Mezon.Sdk.Builders;

/// <summary>
/// Fluent builder for interactive button components.
/// Matches the TypeScript <c>ButtonBuilder</c> class.
/// </summary>
public sealed class ButtonBuilder
{
    private readonly List<IButtonMessage> _buttons = new();

    /// <summary>Add a button with the specified id, label, and style.</summary>
    public ButtonBuilder AddButton(string id, string label, EButtonMessageStyle style)
    {
        _buttons.Add(new IButtonMessage
        {
            Label = label,
            Disable = false,
            Style = (int)style,
            Url = null
        });
        return this;
    }

    /// <summary>Add a primary (blurple) button.</summary>
    public ButtonBuilder AddPrimaryButton(string label, EButtonMessageStyle style = EButtonMessageStyle.Primary)
        => AddButton(label, label, style);

    /// <summary>Add a danger (red) button.</summary>
    public ButtonBuilder AddDangerButton(string label)
        => AddButton(label, label, EButtonMessageStyle.Danger);

    /// <summary>Add a link button.</summary>
    public ButtonBuilder AddLinkButton(string label, string url)
    {
        _buttons.Add(new IButtonMessage
        {
            Label = label,
            Url = url,
            Style = (int)EButtonMessageStyle.Link,
            Disable = false
        });
        return this;
    }

    /// <summary>Build the button message array.</summary>
    public IButtonMessage[] Build() => _buttons.ToArray();
}
