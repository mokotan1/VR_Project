using System;
using System.Collections.Generic;

namespace VRProject.Infrastructure.DI
{
    public sealed class ServiceLocator : IDisposable
    {
        private static ServiceLocator _instance;
        private static readonly object InstanceLock = new();

        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        _instance ??= new ServiceLocator();
                    }
                }
                return _instance;
            }
        }

        private readonly Dictionary<Type, Func<object>> _factories = new();
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly object _lock = new();
        private bool _disposed;

        private ServiceLocator() { }

        public void RegisterSingleton<TInterface>(TInterface implementation) where TInterface : class
        {
            ThrowIfDisposed();
            if (implementation == null)
                throw new ArgumentNullException(nameof(implementation));

            lock (_lock)
            {
                _singletons[typeof(TInterface)] = implementation;
            }
        }

        public void RegisterSingleton<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : TInterface, new()
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                _singletons[typeof(TInterface)] = new TImplementation();
            }
        }

        public void RegisterTransient<TInterface>(Func<TInterface> factory) where TInterface : class
        {
            ThrowIfDisposed();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (_lock)
            {
                _factories[typeof(TInterface)] = factory;
            }
        }

        public TInterface Resolve<TInterface>() where TInterface : class
        {
            ThrowIfDisposed();
            lock (_lock)
            {
                if (_singletons.TryGetValue(typeof(TInterface), out var singleton))
                    return (TInterface)singleton;

                if (_factories.TryGetValue(typeof(TInterface), out var factory))
                    return (TInterface)factory();
            }

            throw new InvalidOperationException(
                $"Service of type {typeof(TInterface).Name} is not registered.");
        }

        public bool IsRegistered<TInterface>() where TInterface : class
        {
            lock (_lock)
            {
                return _singletons.ContainsKey(typeof(TInterface))
                    || _factories.ContainsKey(typeof(TInterface));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_lock)
            {
                foreach (var singleton in _singletons.Values)
                {
                    (singleton as IDisposable)?.Dispose();
                }

                _singletons.Clear();
                _factories.Clear();
            }
        }

        /// <summary>
        /// Resets the static instance. Intended for testing only.
        /// </summary>
        public static void ResetForTesting()
        {
            lock (InstanceLock)
            {
                _instance?.Dispose();
                _instance = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceLocator));
        }
    }
}
