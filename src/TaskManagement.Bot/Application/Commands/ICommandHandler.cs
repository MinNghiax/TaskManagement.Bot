using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands;

public interface ICommandHandler
{
    bool CanHandle(string command);

    Task<CommandResponse> HandleAsync(
        ChannelMessage message,
        CancellationToken cancellationToken);
}