using System;
using System.Collections.Generic;

namespace TacticalDuelist.Platform
{
    /// <summary>
    /// Lightweight service locator for platform services.
    /// Only used for IPlatformService and sub-services — NOT for game logic.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static T Get<T>() where T : class
        {
            return _services.TryGetValue(typeof(T), out var service)
                ? (T)service
                : throw new InvalidOperationException(
                    $"Service {typeof(T).Name} not registered. " +
                    "Ensure PlatformBootstrap runs before any service access.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var raw))
            {
                service = (T)raw;
                return true;
            }
            service = null;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}
