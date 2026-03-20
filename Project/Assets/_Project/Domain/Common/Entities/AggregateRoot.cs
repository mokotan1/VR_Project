using System;
using System.Collections.Generic;
using VRProject.Domain.Common.Events;

namespace VRProject.Domain.Common.Entities
{
    public abstract class AggregateRoot<TId> : EntityBase<TId>
        where TId : IEquatable<TId>
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected AggregateRoot(TId id) : base(id)
        {
        }

        protected void RaiseDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
