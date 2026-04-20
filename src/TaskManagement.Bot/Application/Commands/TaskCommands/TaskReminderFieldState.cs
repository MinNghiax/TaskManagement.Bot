using System.Globalization;
using TaskManagement.Bot.Application.DTOs;
using TaskManagement.Bot.Infrastructure.Enums;

namespace TaskManagement.Bot.Application.Commands.TaskCommands;

public sealed class TaskReminderFieldState
{
    private const double MinWeekValue = 1;
    private const double MaxWeekValue = 4;

    public bool IsEnabled { get; init; } = true;
    public string BeforeValue { get; init; } = "30";
    public ETimeUnit? BeforeUnit { get; init; } = ETimeUnit.Minutes;
    public string AfterValue { get; init; } = "10";
    public ETimeUnit? AfterUnit { get; init; } = ETimeUnit.Minutes;
    public bool IsAfterRepeatEnabled { get; init; } = true;
    public string RepeatValue { get; init; } = string.Empty;
    public ETimeUnit? RepeatUnit { get; init; }

    public static TaskReminderFieldState Default(bool isEnabled = true) => new()
    {
        IsEnabled = isEnabled,
        BeforeValue = isEnabled ? "30" : string.Empty,
        BeforeUnit = isEnabled ? ETimeUnit.Minutes : null,
        AfterValue = isEnabled ? "10" : string.Empty,
        AfterUnit = isEnabled ? ETimeUnit.Minutes : null,
        IsAfterRepeatEnabled = isEnabled,
        RepeatValue = string.Empty,
        RepeatUnit = null
    };

    public static TaskReminderFieldState FromRules(IEnumerable<CreateReminderRuleDto>? rules)
    {
        var ruleList = rules?.ToList() ?? [];
        if (ruleList.Count == 0)
            return Default(isEnabled: false);

        var beforeRule = ruleList.FirstOrDefault(r => r.TriggerType == EReminderTriggerType.BeforeDeadline);
        var afterRule = ruleList.FirstOrDefault(r => r.TriggerType == EReminderTriggerType.AfterDeadline);
        var repeatRule = ruleList.FirstOrDefault(r => r.TriggerType == EReminderTriggerType.Repeat);

        return new TaskReminderFieldState
        {
            IsEnabled = true,
            BeforeValue = beforeRule is null ? string.Empty : FormatValue(beforeRule.Value),
            BeforeUnit = beforeRule?.IntervalUnit,
            AfterValue = afterRule is null ? string.Empty : FormatValue(afterRule.Value),
            AfterUnit = afterRule?.IntervalUnit,
            IsAfterRepeatEnabled = afterRule?.IsRepeat ?? false,
            RepeatValue = repeatRule is null ? string.Empty : FormatValue(repeatRule.Value),
            RepeatUnit = repeatRule?.IntervalUnit
        };
    }

    public TaskReminderValidationResult Validate()
    {
        if (!IsEnabled)
            return TaskReminderValidationResult.Success([]);

        var rules = new List<CreateReminderRuleDto>();
        var errors = new List<string>();

        AddRule(
            rules,
            errors,
            label: "Truoc deadline",
            valueText: BeforeValue,
            unit: BeforeUnit,
            triggerType: EReminderTriggerType.BeforeDeadline,
            isRepeat: false);

        AddRule(
            rules,
            errors,
            label: "Sau deadline",
            valueText: AfterValue,
            unit: AfterUnit,
            triggerType: EReminderTriggerType.AfterDeadline,
            isRepeat: IsAfterRepeatEnabled);

        AddRule(
            rules,
            errors,
            label: "Bao lap",
            valueText: RepeatValue,
            unit: RepeatUnit,
            triggerType: EReminderTriggerType.Repeat,
            isRepeat: true);

        if (errors.Count > 0)
            return TaskReminderValidationResult.Failure(string.Join(Environment.NewLine, errors));

        if (rules.Count == 0)
            return TaskReminderValidationResult.Failure("Khi bat reminder, can cau hinh it nhat 1 loai thong bao hop le.");

        return TaskReminderValidationResult.Success(rules);
    }

    private static void AddRule(
        List<CreateReminderRuleDto> rules,
        List<string> errors,
        string label,
        string? valueText,
        ETimeUnit? unit,
        EReminderTriggerType triggerType,
        bool isRepeat)
    {
        if (string.IsNullOrWhiteSpace(valueText))
            return;

        if (!unit.HasValue)
        {
            errors.Add($"{label}: vui long chon don vi thoi gian.");
            return;
        }

        if (!TryParsePositiveValue(valueText, out var value))
        {
            errors.Add($"{label}: gia tri phai lon hon 0.");
            return;
        }

        if (unit == ETimeUnit.Weeks && (value < MinWeekValue || value > MaxWeekValue))
        {
            errors.Add($"{label}: don vi tuan chi ho tro gia tri tu 1 den 4.");
            return;
        }

        rules.Add(new CreateReminderRuleDto
        {
            TriggerType = triggerType,
            IntervalUnit = unit.Value,
            Value = value,
            IsRepeat = isRepeat
        });
    }

    private static bool TryParsePositiveValue(string valueText, out double value)
    {
        if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return false;
        }

        return value > 0;
    }

    private static string FormatValue(double value)
    {
        return Math.Abs(value - Math.Round(value)) < 0.0001
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}

public sealed record TaskReminderValidationResult(
    bool IsValid,
    string Message,
    IReadOnlyList<CreateReminderRuleDto> Rules)
{
    public static TaskReminderValidationResult Success(IReadOnlyList<CreateReminderRuleDto> rules) =>
        new(true, string.Empty, rules);

    public static TaskReminderValidationResult Failure(string message) =>
        new(false, message, []);
}
