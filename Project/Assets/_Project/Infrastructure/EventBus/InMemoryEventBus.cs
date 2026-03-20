using System;
using System.Collections.Generic;
using VRProject.Domain.Common.Events;
using VRProject.Domain.Common.Interfaces;

namespace VRProject.Infrastructure.EventBus
{
    public sealed class InMemoryEventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _handlers = new();
        private readonly object _lock = new();

        public void Publish<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));

            List<object> handlersCopy;
            lock (_lock)
            {
                if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
                    return;

                handlersCopy = new List<object>(handlers);
            }

            foreach (var handler in handlersCopy)
            {
                switch (handler)
                {
                    case IDomainEventHandler<TEvent> typedHandler:
                        typedHandler.Handle(domainEvent);
                        break;
                    case Action<TEvent> actionHandler:
                        actionHandler(domainEvent);
                        break;
                }
            }
        }

        public IDisposable Subscribe<TEvent>(IDomainEventHandler<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return AddHandler(typeof(TEvent), handler);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
            where TEvent : IDomainEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            return AddHandler(typeof(TEvent), handler);
        }

        private IDisposable AddHandler(Type eventType, object handler)
        {
            lock (_lock)
            {
                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<object>();

                _handlers[eventType].Add(handler);
            }

            return new Subscription(() =>
            {
                lock (_lock)
                {
                    if (_handlers.TryGetValue(eventType, out var handlers))
                        handlers.Remove(handler);
                }
            });
        }

        private sealed class Subscription : IDisposable
        {
            private Action _unsubscribe;

            public Subscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}
