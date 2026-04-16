using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Services;

public class TeamService : ITeamService
{
    private readonly TaskManagementDbContext _context;

    public TeamService(TaskManagementDbContext context)
    {
        _context = context;
    }

    public async Task<Team?> CreateTeamAsync(
        int projectId,
        string teamName,
        string pmUserId,
        List<string> memberUserIds,
        string memberStatus = "Pending",
        CancellationToken cancellationToken = default)
    {
        var projectExists = await _context.Projects.AnyAsync(x => x.Id == projectId, cancellationToken);
        if (!projectExists)
            throw new Exception("Project không tồn tại");

        var team = new Team
        {
            Name = teamName.Trim(),
            CreatedBy = pmUserId,
            ProjectId = projectId
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync(cancellationToken);

        _context.TeamMembers.Add(new TeamMember
        {
            TeamId = team.Id,
            Username = pmUserId,
            Role = "PM",
            Status = "Accepted"
        });

        foreach (var userId in memberUserIds.Distinct())
        {
            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.Id,
                Username = userId,
                Role = "Member",
                Status = memberStatus
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return team;
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return _context.Teams.AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> IsUserInTeam(string username, int teamId)
    {
        return await _context.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.Username == username);
    }

    public async Task<bool> IsPM(string username, int teamId)
    {
        return await _context.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.Username == username && x.Role == "PM");
    }

    public async Task<List<string>> GetMembers(int teamId)
    {
        return await _context.TeamMembers
            .Where(x => x.TeamId == teamId)
            .Select(x => x.Username)
            .ToListAsync();
    }

    public async Task AddMemberAsync(int teamId, string username, string role = "Member")
    {
        // Check team tồn tại
        var team = await _context.Teams.FindAsync(teamId);
        if (team == null)
            throw new Exception("Team không tồn tại");

        // Rule: max 8 member / team
        var memberCount = await _context.TeamMembers
            .CountAsync(x => x.TeamId == teamId);

        if (memberCount >= 8)
            throw new Exception("Team đã đủ 8 thành viên!");

        // Rule: 1 user max 3 team
        var teamCount = await _context.TeamMembers
            .CountAsync(x => x.Username == username);

        if (teamCount >= 3)
            throw new Exception("User đã tham gia tối đa 3 team!");

        // Check duplicate
        var exists = await _context.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.Username == username);

        if (exists)
            throw new Exception("User đã ở trong team này!");

        // Add
        var member = new TeamMember
        {
            TeamId = teamId,
            Username = username,
            Role = role
        };

        _context.TeamMembers.Add(member);
        await _context.SaveChangesAsync();
    }

    public async Task<Team> CreateTeamWithProjectAsync(string projectName, string teamName, string pmUserId, List<string> memberUserIds)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Tạo Project
            var project = new Project
            {
                Name = projectName,
                CreatedBy = pmUserId
            };
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Tạo Team
            var team = new Team
            {
                Name = teamName,
                CreatedBy = pmUserId,
                ProjectId = project.Id
            };
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Thêm PM
            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.Id,
                Username = pmUserId,
                Role = "PM",
                Status = "Accepted"
            });

            // Thêm members
            foreach (var memberId in memberUserIds.Distinct())
            {
                if (memberId != pmUserId)
                {
                    _context.TeamMembers.Add(new TeamMember
                    {
                        TeamId = team.Id,
                        Username = memberId,
                        Role = "Member",
                        Status = "Accepted"
                    });
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return team;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<Team>> GetTeamsByMemberAsync(string username)
    {
        var teamIds = await _context.TeamMembers
            .Where(x => x.Username == username && x.Status == "Accepted")
            .Select(x => x.TeamId)
            .ToListAsync();

        return await _context.Teams
            .Where(t => teamIds.Contains(t.Id) && !t.IsDeleted)
            .ToListAsync();
    }
}
