using Mezon.Sdk.Domain;

namespace Mezon.Sdk.Builders;

public class DynamicFormBuilder
{
    private string _title = "";
    private string _description = "";
    private string _color = "#5865F2";
    private string? _footerText;
    private readonly List<DynamicField> _fields = new();
    private readonly List<DynamicButton> _buttons = new();
    private readonly Dictionary<string, string> _state = new();
    private readonly Dictionary<string, Func<string, Task<SelectOption[]>>> _dependentLoaders = new();

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
        string? placeholder = null,
        string? dependsOn = null,
        Func<string, Task<SelectOption[]>>? optionsLoader = null)
    {
        _fields.Add(new DynamicField
        {
            Type = FieldType.Select,
            Id = id,
            Label = label,
            Options = options,
            Placeholder = placeholder ?? "Select...",
            DependsOn = dependsOn
        });

        if (dependsOn != null && optionsLoader != null)
        {
            _dependentLoaders[id] = optionsLoader;
        }

        return this;
    }

    public DynamicFormBuilder AddInput(
        string id,
        string label,
        string? placeholder = null,
        string? defaultValue = null,
        int? maxLength = null)
    {
        _fields.Add(new DynamicField
        {
            Type = FieldType.Input,
            Id = id,
            Label = label,
            Placeholder = placeholder ?? "",
            DefaultValue = defaultValue,
            MaxLength = maxLength
        });

        return this;
    }

    public DynamicFormBuilder AddText(string label, string value, bool inline = false)
    {
        _fields.Add(new DynamicField
        {
            Type = FieldType.Text,
            Label = label,
            Value = value,
            Inline = inline
        });

        return this;
    }

    public DynamicFormBuilder AddButton(string id, string label, ButtonStyle style = ButtonStyle.Primary)
    {
        _buttons.Add(new DynamicButton
        {
            Id = id,
            Label = label,
            Style = (int)style
        });

        return this;
    }

    public DynamicFormBuilder SetState(string fieldId, string value)
    {
        _state[fieldId] = value;
        return this;
    }

    public string? GetState(string fieldId)
    {
        return _state.TryGetValue(fieldId, out var value) ? value : null;
    }

    public async Task<DynamicFormBuilder> LoadDependentOptionsAsync(string fieldId, string parentValue)
    {
        if (_dependentLoaders.TryGetValue(fieldId, out var loader))
        {
            var options = await loader(parentValue);
            var field = _fields.FirstOrDefault(f => f.Id == fieldId);
            if (field != null)
            {
                field.Options = options;
            }
        }

        return this;
    }

    public ChannelMessageContent Build()
    {
        var embedFields = new List<object>();

        foreach (var field in _fields)
        {
            switch (field.Type)
            {
                case FieldType.Select:
                    embedFields.Add(BuildSelectField(field));
                    break;

                case FieldType.Input:
                    embedFields.Add(BuildInputField(field));
                    break;

                case FieldType.Text:
                    embedFields.Add(new
                    {
                        name = field.Label,
                        value = field.Value,
                        inline = field.Inline
                    });
                    break;
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

    private object BuildSelectField(DynamicField field)
    {
        var selectedValue = GetState(field.Id);

        // If value is selected, show as text (locked)
        if (!string.IsNullOrEmpty(selectedValue))
        {
            var selectedOption = field.Options?.FirstOrDefault(o => o.Value == selectedValue);
            if (selectedOption != null)
            {
                return new
                {
                    name = $"✅ {field.Label}",
                    value = $"**{selectedOption.Label}**",
                    inline = false
                };
            }
        }
        if (!string.IsNullOrEmpty(field.DependsOn))
        {
            var parentValue = GetState(field.DependsOn);
            
            if (string.IsNullOrEmpty(parentValue))
            {
                return new
                {
                    name = field.Label,
                    value = $"_Vui lòng chọn {GetFieldLabel(field.DependsOn)} trước_",
                    inline = false
                };
            }
        }

        return new
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
        };
    }

    private object BuildInputField(DynamicField field)
    {
        var component = new Dictionary<string, object>
        {
            ["id"] = $"{field.Id}_input",
            ["placeholder"] = field.Placeholder ?? "",
            ["defaultValue"] = field.DefaultValue ?? "",
            ["type"] = "text",
            ["textarea"] = false
        };

        if (field.MaxLength.HasValue)
        {
            component["maxLength"] = field.MaxLength.Value;
        }

        return new
        {
            name = field.Label,
            value = field.DefaultValue ?? string.Empty,
            inputs = new
            {
                id = field.Id,
                type = 3,
                component
            }
        };
    }

    private string GetFieldLabel(string fieldId)
    {
        return _fields.FirstOrDefault(f => f.Id == fieldId)?.Label ?? fieldId;
    }

    public DynamicFormBuilder Clone()
    {
        var clone = new DynamicFormBuilder
        {
            _title = _title,
            _description = _description,
            _color = _color,
            _footerText = _footerText
        };

        foreach (var field in _fields)
        {
            clone._fields.Add(field);
        }

        foreach (var button in _buttons)
        {
            clone._buttons.Add(button);
        }

        foreach (var kvp in _state)
        {
            clone._state[kvp.Key] = kvp.Value;
        }

        foreach (var kvp in _dependentLoaders)
        {
            clone._dependentLoaders[kvp.Key] = kvp.Value;
        }

        return clone;
    }
}

public class DynamicField
{
    public FieldType Type { get; set; }
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Value { get; set; }
    public SelectOption[]? Options { get; set; }
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public int? MaxLength { get; set; }
    public bool Inline { get; set; }
    public string? DependsOn { get; set; }
}

public class SelectOption
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";

    public SelectOption() { }

    public SelectOption(string label, string value)
    {
        Label = label;
        Value = value;
    }
}

public class DynamicButton
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public int Style { get; set; }
}

public enum FieldType
{
    Select,
    Input,
    Text
}

public enum ButtonStyle
{
    Primary = 1,
    Secondary = 2,
    Success = 3,
    Danger = 4,
    Link = 5
}
