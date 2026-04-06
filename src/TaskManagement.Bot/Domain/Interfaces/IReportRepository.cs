namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;

/// <summary>
/// Report-specific repository interface
/// </summary>
public interface IReportRepository : IRepository<Report>
{
    Task<IEnumerable<Report>> GetByReportTypeAsync(string reportType);
    Task<IEnumerable<Report>> GetByCreatorAsync(string createdBy);
    Task<IEnumerable<Report>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
