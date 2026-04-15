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
        Task<Project> CreateProjectAsync(string name, string description, string creator, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
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

        public async Task<Project> CreateProjectAsync(string name, string description, string creator, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new Exception("Tên project không được để trống");

            var project = new Project
            {
                Name = name.Trim(),
                CreatedBy = creator
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);

            return project;
        }

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim().ToLower();
            return _context.Projects.AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == normalized, cancellationToken);
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
