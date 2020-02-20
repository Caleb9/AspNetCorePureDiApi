using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using AspNetCorePureDiApi.Controllers;
using AspNetCorePureDiApi.Middlewares;
using AspNetCorePureDiApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
/* Short name to limit line width */
using MiddlewareDependenciesDictionary = System.Collections.Concurrent.ConcurrentDictionary<
    Microsoft.AspNetCore.Http.IMiddleware, System.Collections.Generic.ICollection<System.IDisposable>>;

namespace AspNetCorePureDiApi.PureDi
{
    /// <summary>
    ///     An example of Pure DI composition root for ASP.NET Core application.
    ///     Creates instances of middlewares and controllers with their dependencies.
    ///     Holds references to singleton dependencies. If any of them implement IDisposable, then they
    ///     need to get disposed when the application shuts down. That's why CompositionRoot implements
    ///     IDisposable itself. This is just an example, but in a real life project the responsibility of storing
    ///     the singleton dependencies should be delegated to another class probably.
    /// </summary>
    /// <remarks>
    ///     Possibly this is an overkill as when the application shuts down all the memory gets reclaimed either way.
    ///     However it's a good practice to always dispose of everything that is IDisposable
    /// </remarks>
    public sealed class CompositionRoot
        : IMiddlewareFactory, IControllerActivator, IDisposable
    {
        /// <summary>
        ///     Ensures that this instance along with associated disposables can be disposed by <see cref="Program" />
        ///     when the application is shutdown. Just by registering CompositionRoot in Startup's
        ///     IServiceCollection does not dispose of it automatically.
        ///     The field is internal so it is not used outside of this project. Integration tests should be able to
        ///     construct their own copy of CompositionRoot, probably with IDisposable dependencies swapped with
        ///     fakes (as it indicates that they access out-of-process resources). Such scenario would call for more
        ///     complex solution where those dependencies are accessible for overwriting in tests.
        /// </summary>
        internal static readonly CompositionRoot Singleton = new CompositionRoot();

        /// <summary>
        ///     An example of a singleton, disposable object used in controller's or middleware's dependency graph.
        /// </summary>
        private readonly DisposableDependency _singletonDisposableDependency;

        /// <summary>
        ///     Singleton dependencies that should be disposed on application shutdown are added to this list.
        /// </summary>
        private readonly List<IDisposable> _singletonDisposables = new List<IDisposable>();
        
        /// <summary>
        ///     Request scoped dependencies for middlewares.
        /// </summary>
        private readonly MiddlewareDependenciesDictionary _disposableScopedMiddlewareDependencies =
            new MiddlewareDependenciesDictionary();
        
        private readonly AsyncLocal<ScopedDependencies> _scopedDependencies = new AsyncLocal<ScopedDependencies>();

        private bool _isDisposed;

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global") /* Used implicitly in Startup */]
        public CompositionRoot()
        {
            _singletonDisposableDependency = RegisterSingletonForDispose(new DisposableDependency());
        }

        private T RegisterSingletonForDispose<T>(T disposableSingleton)
            where T : IDisposable
        {
            _singletonDisposables.Add(disposableSingleton);
            return disposableSingleton;
        }
        
        IMiddleware IMiddlewareFactory.Create(Type middlewareType)
        {
            AssertNotDisposed();
            if (middlewareType == typeof(MyMiddleware))
            {
                var scopedDependency = new DisposableDependency();
                _scopedDependencies.Value = new ScopedDependencies();
                _scopedDependencies.Value.Add<IDependency>(scopedDependency);
                var middleware =
                    new MyMiddleware(
                        _singletonDisposableDependency,
                        scopedDependency);
                RegisterForDispose(middleware, scopedDependency);
                return middleware;
            }

            throw new InvalidOperationException("Unknown middleware type");
        }
        
        /// <summary>
        ///     Register disposable dependencies for a middleware.
        /// </summary>
        private void RegisterForDispose(IMiddleware middleware, params IDisposable[] scopedDisposables)
        {
            var disposables =
                _disposableScopedMiddlewareDependencies.GetOrAdd(middleware,
                    new List<IDisposable>(scopedDisposables.Length));
            foreach (var disposable in scopedDisposables)
            {
                disposables.Add(disposable);
            }
        }
        
        void IMiddlewareFactory.Release(IMiddleware middleware)
        {
            if (!_disposableScopedMiddlewareDependencies.TryRemove(middleware, out var disposables))
            {
                return;
            }

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            _scopedDependencies.Value = null;
        }

        object IControllerActivator.Create(ControllerContext context)
        {
            AssertNotDisposed();

            if (GetControllerType() == typeof(HelloController))
            {
                // var scopedDependency = RegisterForDispose(context, new DisposableDependency());
                var scopedDependency = _scopedDependencies.Value.Get<IDependency>();
                return new HelloController(
                    _singletonDisposableDependency,
                    scopedDependency);
            }

            throw new InvalidOperationException("Unknown controller type");

            Type GetControllerType()
            {
                return context.ActionDescriptor.ControllerTypeInfo.AsType();
            }
        }

        /// <summary>
        ///     <paramref name="scopedDisposable" /> will be disposed when request handling by a controller is finished.
        /// </summary>
        private T RegisterForDispose<T>(ActionContext context, T scopedDisposable)
            where T : IDisposable
        {
            context.HttpContext.Response.RegisterForDispose(scopedDisposable);
            return scopedDisposable;
        }

        void IControllerActivator.Release(ControllerContext context, object controller)
        {
            /* Not used as we register disposables for dispose by ASP.NET Core framework in HttpContext */
        }

        public void Dispose()
        {
            /* Dispose any leftover middleware disposables. This can only matter if application is shut-down in the
             * middle of handling a request (not sure if that can ever happen). */
            var disposableMiddlewareDependencies =
             _disposableScopedMiddlewareDependencies.Values.SelectMany(disposables => disposables);
            foreach (var disposable in disposableMiddlewareDependencies)
            {
                disposable.Dispose();
            }
            /* Dispose singletons */
            foreach (var disposable in _singletonDisposables)
            {
                disposable.Dispose();
            }

            _isDisposed = true;
        }

        /// <summary>
        ///     We should not be able to re-use a disposed instance of this class. All the singletons are already
        ///     disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void AssertNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(ToString());
            }
        }
    }
    
    class ScopedDependencies
    {
        private readonly Dictionary<Type, object> _dependencies = new Dictionary<Type, object>();

        public void Add<T>(T dependency)
        {
            _dependencies.Add(typeof(T), dependency);
        }
        
        public T Get<T>()
        {
            return (T) _dependencies[typeof(T)];
        }
    }
}