using System.Collections.Generic;
using System.Threading.Tasks;

namespace App.SharedKernel.Interfaces;

public interface IRepository<T> where T : BaseEntity, IAggregateRoot {
    //    Task<T> GetByIdAsync<T>(int id) where T : BaseEntity, IAggregateRoot;
    Task<List<T>> ListAsync();
    Task<int> GetCountAsync();
//    Task<List<T>> ListAsync<T>(ISpecification<T> spec) where T : BaseEntity, IAggregateRoot;
//    Task<T> AddAsync<T>(T entity) where T : BaseEntity, IAggregateRoot;
//    Task UpdateAsync<T>(T entity) where T : BaseEntity, IAggregateRoot;
//    Task DeleteAsync<T>(T entity) where T : BaseEntity, IAggregateRoot;
}
