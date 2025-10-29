using System.Linq.Expressions;

namespace PracticalWork.Library.Abstractions.Storage;

public interface IQueryBuilder<TEntity> 
    where TEntity : EntityBase
{
    IQueryBuilder<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> expression, bool ascending);
    IQueryBuilder<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
    IQueryBuilder<TEntity> Take(int count);
    Task<List<TEntity>> ExecuteQuery();
}