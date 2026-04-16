using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services
{
    public interface ITeamService
    {
        Task<Team?> CreateTeamAsync(
            int projectId,
            string name,
            string owner,
            List<string> members,
            string memberStatus = "Pending",
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<bool> IsUserInTeam(string username, int teamId);

        Task<bool> IsPM(string username, int teamId);

        Task<List<string>> GetMembers(int teamId);

        Task AddMemberAsync(int teamId, string username, string role = "Member");

        Task<Team> CreateTeamWithProjectAsync(string projectName, string teamName, string pmUserId, List<string> memberUserIds);

        Task<List<Team>> GetTeamsByMemberAsync(string username);
    }
}
