
    public interface IEntityRepository<T> : IDisposable
       where T : Entity
    {
        Task AddAsync(T entity);
        Task DeleteAsync(Guid id);
        Task<T?> GetByIdAsync(Guid id);
        Task UpdateAsync(T entity);
        Task<IEnumerable<T>> List();
        Task<long> CountAsync();
    }

    public interface IEntityRepositoryDemo<T> : IEntityRepository<T>
       where T : Entity
    {
        Task<Guid?> GetRandomIdAsync();
        Task<T> GetRandomEntityAsync();
    }
