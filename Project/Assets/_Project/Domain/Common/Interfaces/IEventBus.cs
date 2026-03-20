using System;
using VRProject.Domain.Common.Events;

namespace VRProject.Domain.Common.Interfaces
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
        IDisposable Subscribe<TEvent>(IDomainEventHandler<TEvent> handler) where TEvent : IDomainEvent;
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent;
    }
}
