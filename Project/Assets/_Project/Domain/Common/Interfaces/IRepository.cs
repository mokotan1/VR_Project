using System;
using System.Collections.Generic;

namespace VRProject.Domain.Common.Interfaces
{
    public interface IRepository<TEntity, in TId>
        where TEntity : class
        where TId : IEquatable<TId>
    {
        TEntity GetById(TId id);
        IReadOnlyList<TEntity> GetAll();
        void Add(TEntity entity);
        void Update(TEntity entity);
        void Remove(TId id);
        bool Exists(TId id);
    }
}
