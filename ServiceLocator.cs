using System;
using System.Collections.Generic;

namespace NewUI
{
    public sealed class ServiceLocator
    {
        private static readonly ServiceLocator _instance = new ServiceLocator();
        public static ServiceLocator Instance => _instance;

        private readonly Dictionary<Type, object> _services = new();

        private ServiceLocator()
        {
            // 预注册默认实例（可按需覆盖）
            Register(new NewUI.Managers.CurrentDropManager());
            Register(new NewUI.Managers.FarmingCostManager());
            Register(new NewUI.Managers.TradingManager());
            Register(new NewUI.Managers.PriceManager());
        }

        public void Register<T>(T service)
        {
            _services[typeof(T)] = service!;
        }

        public T Get<T>() where T : class, new()
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            var inst = new T();
            _services[typeof(T)] = inst;
            return inst;
        }

        public bool TryGet<T>(out T? service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
            service = null; return false;
        }

        public void RegisterSingleton<T>(T impl) where T : class
        {
            _services[typeof(T)] = impl ?? throw new ArgumentNullException(nameof(impl));
        }

    }
}
