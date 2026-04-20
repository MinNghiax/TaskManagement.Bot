using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.Common;

public class DynamicFormBuilder
{
    private string _title = "";
    private string _description = "";
    private string _color = "#5865F2";
    private readonly List<FormField> _fields = new();
    private readonly List<FormButton> _buttons = new();
    private string? _footerText;
    private readonly Dictionary<string, string> _selectedValues = new();

    public DynamicFormBuilder SetTitle(string title)
    {
        _title = title;
        return this;
    }

    public DynamicFormBuilder SetDescription(string description)
    {
        _description = description;
        return this;
    }

    public DynamicFormBuilder SetColor(string color)
    {
        _color = color;
        return this;
    }

    public DynamicFormBuilder SetFooter(string text)
    {
        _footerText = text;
        return this;
    }

    public DynamicFormBuilder AddSelect(
        string id,
        string label,
        SelectOption[] options,
        string? selectedValue = null,
        string? placeholder = null)
    {
        _fields.Add(new FormField
        {
            Type = FormFieldType.Select,
            Id = id,
            Label = label,
            Options = options,
            SelectedValue = selectedValue,
            Placeholder = placeholder ?? "Chọn..."
        });

        if (selectedValue != null)
        {
            _selectedValues[id] = selectedValue;
        }

        return this;
    }

    public DynamicFormBuilder AddTextField(string label, string value, bool inline = false)
    {
        _fields.Add(new FormField
        {
            Type = FormFieldType.Text,
            Label = label,
            Value = value,
            Inline = inline
        });

        return this;
    }

    public DynamicFormBuilder AddButton(string id, string label, int style)
    {
        _buttons.Add(new FormButton
        {
            Id = id,
            Label = label,
            Style = style
        });

        return this;
    }

    public string? GetSelectedValue(string fieldId)
    {
        return _selectedValues.TryGetValue(fieldId, out var value) ? value : null;
    }

    public ChannelMessageContent Build()
    {
        var embedFields = new List<object>();

        foreach (var field in _fields)
        {
            if (field.Type == FormFieldType.Select)
            {
                // If value is selected, show as text instead of dropdown
                if (!string.IsNullOrEmpty(field.SelectedValue))
                {
                    var selectedOption = field.Options?.FirstOrDefault(o => o.Value == field.SelectedValue);
                    if (selectedOption != null)
                    {
                        embedFields.Add(new
                        {
                            name = $"✅ {field.Label}",
                            value = $"**{selectedOption.Label}**",
                            inline = false
                        });
                        continue;
                    }
                }

                // Show as dropdown
                embedFields.Add(new
                {
                    name = field.Label,
                    value = string.Empty,
                    inputs = new
                    {
                        id = field.Id,
                        type = 2,
                        component = new
                        {
                            placeholder = field.Placeholder,
                            options = field.Options?.Select(o => new
                            {
                                label = o.Label,
                                value = o.Value
                            }).ToArray() ?? Array.Empty<object>()
                        }
                    }
                });
            }
            else if (field.Type == FormFieldType.Text)
            {
                embedFields.Add(new
                {
                    name = field.Label,
                    value = field.Value,
                    inline = field.Inline
                });
            }
        }

        var components = _buttons.Select(b => new
        {
            id = b.Id,
            type = 1,
            component = new
            {
                label = b.Label,
                style = b.Style
            }
        }).ToArray();

        var embed = new
        {
            title = _title,
            description = _description,
            color = _color,
            fields = embedFields.ToArray(),
            footer = _footerText != null ? new { text = _footerText } : null
        };

        return new ChannelMessageContent
        {
            Embed = new[] { embed },
            Components = components.Length > 0 ? new[] { new { components } } : null
        };
    }
}

public class FormField
{
    public FormFieldType Type { get; set; }
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Value { get; set; }
    public SelectOption[]? Options { get; set; }
    public string? SelectedValue { get; set; }
    public string? Placeholder { get; set; }
    public bool Inline { get; set; }
}

public class SelectOption
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

public class FormButton
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public int Style { get; set; }
}

public enum FormFieldType
{
    Select,
    Text,
    Input
}
