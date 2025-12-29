using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SupportTools.Domain.Primitives;

namespace SupportTools.Domain.Repositories;

public interface ICrudRepository<T, TId> where T : Entity<TId> where TId : notnull
{
    Task<List<T>> GetAll(CancellationToken cancellationToken);
    void Delete(T o);
    void Add(T crudEntity);
    void Update(T crudEntity);
}