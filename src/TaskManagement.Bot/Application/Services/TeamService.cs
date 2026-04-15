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

    public async Task<Team?> CreateTeamAsync(int projectId, string teamName, string pmUserId, List<string> memberUserIds)
    {
        var projectExists = await _context.Projects.AnyAsync(x => x.Id == projectId);
        if (!projectExists)
            throw new Exception("Project không tồn tại");

        var team = new Team
        {
            Name = teamName,
            CreatedBy = pmUserId,
            ProjectId = projectId
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        //  PM = Accepted
        _context.TeamMembers.Add(new TeamMember
        {
            TeamId = team.Id,
            Username = pmUserId, 
            Role = "PM",
            Status = "Accepted"
        });

        //  Members = Pending
        foreach (var userId in memberUserIds.Distinct())
        {
            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = team.Id,
                Username = userId,
                Role = "Member",
                Status = "Pending"
            });
        }

        await _context.SaveChangesAsync();
        return team;
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
}
