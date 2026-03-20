using System;
using UnityEngine;
using VRProject.Domain.Common.Interfaces;
using VRProject.Infrastructure.DI;

namespace VRProject.Presentation.Common.Managers
{
    /// <summary>
    /// Base class for controllers that mediate between the Presentation and Application layers.
    /// Resolves services from the DI container and manages subscriptions to domain events.
    /// </summary>
    public abstract class ControllerBase : MonoBehaviour
    {
        private readonly CompositeDisposable _subscriptions = new();

        protected IEventBus EventBus { get; private set; }

        protected virtual void Awake()
        {
            EventBus = ServiceLocator.Instance.Resolve<IEventBus>();
        }

        protected void AddSubscription(IDisposable subscription)
        {
            _subscriptions.Add(subscription);
        }

        protected T Resolve<T>() where T : class
        {
            return ServiceLocator.Instance.Resolve<T>();
        }

        protected virtual void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private sealed class CompositeDisposable : IDisposable
        {
            private readonly System.Collections.Generic.List<IDisposable> _disposables = new();
            private bool _disposed;

            public void Add(IDisposable disposable)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CompositeDisposable));

                _disposables.Add(disposable);
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                for (int i = _disposables.Count - 1; i >= 0; i--)
                {
                    _disposables[i]?.Dispose();
                }
                _disposables.Clear();
            }
        }
    }
}
