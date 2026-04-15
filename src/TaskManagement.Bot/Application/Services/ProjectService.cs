using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Bot.Infrastructure.Data;
using TaskManagement.Bot.Infrastructure.Entities;

namespace TaskManagement.Bot.Application.Services
{
    public interface IProjectService
    {
        Task<Project> CreateProjectAsync(string name, string description, string creator);
        Task<List<Project>> GetAllProjectsAsync();
        Task<Project?> GetProjectByIdAsync(int id);
    }

    public class ProjectService : IProjectService
    {
        private readonly TaskManagementDbContext _context;

        public ProjectService(TaskManagementDbContext context)
        {
            _context = context;
        }

        //  Tạo Project + auto PM
        public async Task<Project> CreateProjectAsync(string name, string description, string creator)
        {
            //  Validate
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Tên project không được để trống");

            //  Tạo Project
            var project = new Project
            {
                Name = name,
                CreatedBy = creator
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            //  Tạo Team mặc định
            var team = new Team
            {
                Name = $"{name} Team",
                CreatedBy = creator,
                ProjectId = project.Id
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            //  Add creator làm PM
            var member = new TeamMember
            {
                TeamId = team.Id,
                Username = creator,
                Role = "PM"
            };

            _context.TeamMembers.Add(member);
            await _context.SaveChangesAsync();

            return project;
        }

        //  Lấy danh sách project
        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .Include(p => p.Teams)
                .ToListAsync();
        }

        //  Lấy chi tiết project
        public async Task<Project?> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Teams)
                    .ThenInclude(t => t.Members)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
