namespace TaskManagement.Bot.Domain.Interfaces;

using TaskManagement.Bot.Infrastructure.Entities;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Func<T, bool> predicate);
    Task<int> CountAsync();

    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAsync(T entity);

    Task<int> SaveChangesAsync();
}
