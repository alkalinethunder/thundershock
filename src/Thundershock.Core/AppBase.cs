using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Thundershock.Core.Debugging;
using Thundershock.Debugging;

namespace Thundershock.Core
{
    public abstract class AppBase
    {
        private List<IGlobalComponent> _components = new List<IGlobalComponent>();
        private Logger _logger;
        private ConcurrentQueue<Action> _actionQueue = new();

        /// <summary>
        /// Submit a method to run on the next Thundershock engine tick. Useful for performing thread-unsafe operations from outside the game thread.
        /// </summary>
        /// <param name="action">The method to run.</param>
        public void EnqueueAction(Action action)
        {
            _actionQueue.Enqueue(action);
        }

        public Logger Logger
        {
            get => _logger;
            internal set => _logger = value;
        }

        public bool Exit()
        {
            Logger.Log("Exit requested.");

            Logger.Log("Allowing the app to do stuff before exiting...");
            var exitEvent = new AppExitEventArgs();
            BeforeExit(exitEvent);

            if (exitEvent.CancelExit)
                Logger.Log("App has cancelled the exit event.");

            return !exitEvent.CancelExit;
        }

        public T GetComponent<T>() where T : IGlobalComponent, new()
        {
            return _components.OfType<T>().First() ?? RegisterComponent<T>();
        }

        public void Run(Logger logger)
        {
            _logger = logger;

            Bootstrap();
        }

        protected void RegisterComponent(IGlobalComponent component)
        {
            _components.Add(component);
            component.Initialize(this);
        }

        protected T RegisterComponent<T>() where T : IGlobalComponent, new()
        {
            if (_components.Any(x => x is T))
                throw new InvalidOperationException("Component is already registered.");

            Logger.Log($"Registering global component: {typeof(T).FullName}");

            var instance = new T();
            RegisterComponent(instance);
            return instance;
        }

        protected void UpdateComponents(GameTime gameTime)
        {
            foreach (var component in _components.ToArray())
            {
                component.Update(gameTime);
            }
        }

        protected void RunQueuedActions()
        {
            while (_actionQueue.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        protected void UnloadAllComponents()
        {
            // Unload all global components.
            while (_components.Any())
            {
                _components.First().Unload();
                _components.RemoveAt(0);
            }
        }


        protected abstract void Bootstrap();
        protected virtual void BeforeExit(AppExitEventArgs args) {}
    }
}
