using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace MediCare.Data.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> Set;

    public GenericRepository(ApplicationDbContext context)
    {
        Context = context;
        Set = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id) => await Set.FindAsync(id);

    public async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await Set.Where(predicate).ToListAsync();

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) =>
        Set.FirstOrDefaultAsync(predicate);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Set.AnyAsync(predicate);

    public async Task AddAsync(T entity) => await Set.AddAsync(entity);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
