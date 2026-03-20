using System;
using System.Collections.Generic;
using System.Linq;
using VRProject.Domain.Common.Interfaces;

namespace VRProject.Infrastructure.Persistence
{
    public class InMemoryRepository<TEntity, TId> : IRepository<TEntity, TId>
        where TEntity : class
        where TId : IEquatable<TId>
    {
        private readonly Dictionary<TId, TEntity> _store = new();
        private readonly Func<TEntity, TId> _idSelector;

        public InMemoryRepository(Func<TEntity, TId> idSelector)
        {
            _idSelector = idSelector ?? throw new ArgumentNullException(nameof(idSelector));
        }

        public TEntity GetById(TId id)
        {
            _store.TryGetValue(id, out var entity);
            return entity;
        }

        public IReadOnlyList<TEntity> GetAll()
        {
            return _store.Values.ToList().AsReadOnly();
        }

        public void Add(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var id = _idSelector(entity);
            if (_store.ContainsKey(id))
                throw new InvalidOperationException(
                    $"Entity with id '{id}' already exists.");

            _store[id] = entity;
        }

        public void Update(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var id = _idSelector(entity);
            if (!_store.ContainsKey(id))
                throw new InvalidOperationException(
                    $"Entity with id '{id}' does not exist.");

            _store[id] = entity;
        }

        public void Remove(TId id)
        {
            if (!_store.Remove(id))
                throw new InvalidOperationException(
                    $"Entity with id '{id}' does not exist.");
        }

        public bool Exists(TId id)
        {
            return _store.ContainsKey(id);
        }
    }
}
