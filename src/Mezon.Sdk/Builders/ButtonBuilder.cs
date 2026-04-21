using Mezon.Sdk.Domain;
using Mezon.Sdk.Enums;

namespace Mezon.Sdk.Builders;

public sealed class ButtonBuilder
{
    private readonly List<IButtonMessage> _buttons = new();

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

    public ButtonBuilder AddPrimaryButton(string label, EButtonMessageStyle style = EButtonMessageStyle.Primary)
        => AddButton(label, label, style);

    public ButtonBuilder AddDangerButton(string label)
        => AddButton(label, label, EButtonMessageStyle.Danger);

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

    public IButtonMessage[] Build() => _buttons.ToArray();
}
