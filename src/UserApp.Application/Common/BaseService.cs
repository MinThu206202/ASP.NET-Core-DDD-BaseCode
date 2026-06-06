using UserApp.Domain.Common;

namespace UserApp.Application.Common
{
    public class BaseService<T> : IBaseService<T> where T : class
    {
        protected readonly IBaseRepository<T> _repo;

        public BaseService(IBaseRepository<T> repo)
        {
            _repo = repo;
        }

        public Task<T?> GetByIdAsync(Guid id) => _repo.GetByIdAsync(id);
        public Task<List<T>> ListAsync(int skip, int take) => _repo.ListAsync(skip, take);
        public Task<int> CountAsync() => _repo.CountAsync();

        public Task AddAsync(T entity) => _repo.AddAsync(entity);
        public async Task UpdateAsync(T entity)
        {
            _repo.Update(entity);
            await _repo.SaveChangesAsync();
        }

        public async Task RemoveAsync(T entity)
        {
            // If entity supports soft delete
            var deletable = entity as dynamic;

            if (deletable != null)
            {
                deletable.Delete();
                _repo.Update(entity);
            }
            else
            {
                _repo.Remove(entity);
            }

            await _repo.SaveChangesAsync();
        }
        public Task SaveAsync() => _repo.SaveChangesAsync();
    }
}