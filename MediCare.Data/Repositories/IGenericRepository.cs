using System.Linq.Expressions;

namespace MediCare.Data.Repositories;

// SHARED: generic CRUD repository. If a teammate already wrote one, keep theirs
// (the Medical Records code only uses the members below).
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
