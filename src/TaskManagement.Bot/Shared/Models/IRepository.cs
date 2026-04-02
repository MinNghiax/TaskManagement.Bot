namespace TaskManagement.Bot.Shared.Models;

/// <summary>
/// Generic repository interface
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> ListAsync();
    Task<List<T>> SearchAsync(Func<T, bool> predicate);
}
