using Mezon.Sdk.Domain;
using Mezon.Sdk.Interfaces;

namespace Mezon.Sdk.Builders;

public class FormUpdateHelper
{
    private readonly MezonClient _client;
    private readonly Dictionary<string, FormContext> _contexts = new();

    public FormUpdateHelper(MezonClient client)
    {
        _client = client;
    }

    public void RegisterForm(string userId, string messageId, DynamicFormBuilder builder)
    {
        _contexts[$"{userId}:{messageId}"] = new FormContext
        {
            UserId = userId,
            MessageId = messageId,
            Builder = builder,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateFormAsync(
        string userId,
        string messageId,
        string clanId,
        string channelId,
        int mode,
        bool isPublic,
        string fieldId,
        string value)
    {
        var key = $"{userId}:{messageId}";
        
        if (!_contexts.TryGetValue(key, out var context))
        {
            return false;
        }
        context.Builder.SetState(fieldId, value);

        var dependentFields = GetDependentFields(context.Builder, fieldId);
        foreach (var depField in dependentFields)
        {
            await context.Builder.LoadDependentOptionsAsync(depField, value);
        }

        var newContent = context.Builder.Build();

        try
        {
            await _client.UpdateMessageAsync(
                clanId,
                channelId,
                mode,
                isPublic,
                messageId,
                newContent);

            context.LastUpdated = DateTime.UtcNow;
            return true;
        }
        catch
        {
            return false;
        }
    }
    public FormContext? GetContext(string userId, string messageId)
    {
        var key = $"{userId}:{messageId}";
        return _contexts.TryGetValue(key, out var context) ? context : null;
    }

    public void UnregisterForm(string userId, string messageId)
    {
        var key = $"{userId}:{messageId}";
        _contexts.Remove(key);
    }

    public void CleanupOldContexts()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var oldKeys = _contexts
            .Where(kvp => kvp.Value.LastUpdated < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldKeys)
        {
            _contexts.Remove(key);
        }
    }

    private List<string> GetDependentFields(DynamicFormBuilder builder, string parentFieldId)
    {
        return new List<string>();
    }
}

public class FormContext
{
    public string UserId { get; set; } = "";
    public string MessageId { get; set; } = "";
    public DynamicFormBuilder Builder { get; set; } = null!;
    public DateTime LastUpdated { get; set; }
}
