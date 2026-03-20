using UnityEngine;
using VRProject.Domain.Common.Interfaces;
using VRProject.Infrastructure.DI;
using VRProject.Infrastructure.EventBus;

namespace VRProject.Presentation.Common.Managers
{
    /// <summary>
    /// Entry point for the application. Initializes the DI container
    /// and registers all infrastructure services before any gameplay begins.
    /// Place this on a GameObject in the first loaded scene.
    /// </summary>
    public sealed class GameBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RegisterCoreServices();
        }

        private static void RegisterCoreServices()
        {
            var locator = ServiceLocator.Instance;

            if (!locator.IsRegistered<IEventBus>())
            {
                locator.RegisterSingleton<IEventBus>(new InMemoryEventBus());
            }
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance.Dispose();
        }
    }
}
