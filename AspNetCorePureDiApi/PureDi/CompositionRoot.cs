using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCorePureDiApi.Controllers;
using AspNetCorePureDiApi.Middlewares;
using AspNetCorePureDiApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
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
    ///     However it's a good practice to always dispose of everything that is IDisposable.
    ///     There is a flaw in current implementation that middlewares and controllers do not share request scoped
    ///     dependencies. It is not trivial to solve this problem without giving up compile time checking of dependency
    ///     types (i.e. not reinventing the wheel by implementing an actual DI container). How should request scoped
    ///     dependencies be shared between IControllerActivator.Create and IMiddlewareFactory.Create?
    /// </remarks>
    public sealed class CompositionRoot
        : IMiddlewareFactory, IControllerActivator, IDisposable
    {
        /// <summary>
        ///     Request scoped dependencies for middlewares.
        /// </summary>
        private readonly MiddlewareDependenciesDictionary _disposableScopedMiddlewareDependencies =
            new MiddlewareDependenciesDictionary();

        /// <summary>
        ///     An example of a singleton object used in controller's or middleware's dependency graph.
        /// </summary>
        private readonly IDependency _singletonDependency;

        /// <summary>
        ///     Singleton dependencies that should be disposed on application shutdown are added to this list.
        /// </summary>
        private readonly List<IDisposable> _singletonDisposables = new List<IDisposable>();

        /// <summary>
        ///     To enable testing if disposable, scoped dependencies get disposed we can inject a factory of these
        ///     dependencies. This is a compromise between testability and purity of the code (i.e. a testing concern
        ///     is visible in "production" code.
        /// </summary>
        private readonly Func<IDependency>? _testingScopedDependencyFactory;

        private bool _isCompositionRootDisposed;

        /// <summary>
        ///     This constructor enables injection of dependencies for testing purposes. In real life scenario it would
        ///     probably be enough to limit this kind of injection to out-of-process "collaborators", e.g. repositories
        ///     communicating with databases, as we would typically want to substitute them with test doubles in tests.
        /// </summary>
        /// <param name="testingSingletonDependency">Inject for testing purposes</param>
        /// <param name="testingScopedDependencyFactory">Inject for testing purposes</param>
        public CompositionRoot(
            IDependency? testingSingletonDependency = default,
            Func<IDependency>? testingScopedDependencyFactory = default)
        {
            _singletonDependency =
                RegisterSingletonDependencyForDispose(
                    testingSingletonDependency ?? new DisposableDependency());
            _testingScopedDependencyFactory = testingScopedDependencyFactory;
        }

        object IControllerActivator.Create(ControllerContext context)
        {
            AssertNotDisposed();

            if (GetControllerType() != typeof(HelloController))
            {
                throw new InvalidOperationException("Unknown controller type");
            }

            var scopedDependency = RegisterScopedDependencyForDispose(context, NewScopedDependency());
            return new HelloController(
                _singletonDependency,
                scopedDependency);

            Type GetControllerType()
            {
                return context.ActionDescriptor.ControllerTypeInfo.AsType();
            }
        }

        void IControllerActivator.Release(ControllerContext context, object controller)
        {
            /* Not used as we register disposables for dispose by ASP.NET Core framework in HttpContext */
        }

        public void Dispose()
        {
            if (_isCompositionRootDisposed)
            {
                /* When ASP.NET Core container gets disposed it's also disposing everything registered in it. Since we
                 * register CompositionRoot as itself, as IMiddlewareFactory and as IControllerActivator, this method
                 * gets invoked multiple times. */
                return;
            }

            DisposeLeftoverScopedMiddlewareDependencies();

            /* Dispose singletons */
            foreach (var disposable in _singletonDisposables)
            {
                disposable.Dispose();
            }

            _isCompositionRootDisposed = true;

            void DisposeLeftoverScopedMiddlewareDependencies()
            {
                /* Dispose any leftover middleware disposables. This potentially can matter if application is shut-down
                 * before it finished handling a request. */
                var disposableMiddlewareDependencies =
                    _disposableScopedMiddlewareDependencies.Values
                        .SelectMany(disposables => disposables);
                foreach (var disposable in disposableMiddlewareDependencies)
                {
                    disposable.Dispose();
                }
            }
        }

        IMiddleware IMiddlewareFactory.Create(Type middlewareType)
        {
            AssertNotDisposed();

            if (middlewareType != typeof(MyMiddleware))
            {
                throw new InvalidOperationException("Unknown middleware type");
            }

            var scopedDependency = NewScopedDependency();
            var middleware =
                new MyMiddleware(
                    _singletonDependency,
                    scopedDependency);
            RegisterScopedDependencyForDispose(middleware, scopedDependency);
            return middleware;
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
        }

        private IDependency NewScopedDependency()
        {
            return _testingScopedDependencyFactory?.Invoke() ?? new DisposableDependency();
        }

        private T RegisterSingletonDependencyForDispose<T>(T singletonDependency)
        {
            if (singletonDependency is IDisposable disposableSingleton)
            {
                _singletonDisposables.Add(disposableSingleton);
            }

            return singletonDependency;
        }

        /// <summary>
        ///     <paramref name="scopedDependency" /> will be disposed when request handling by a controller is finished.
        /// </summary>
        private T RegisterScopedDependencyForDispose<T>(ActionContext context, T scopedDependency)
        {
            if (scopedDependency is IDisposable scopedDisposable)
            {
                context.HttpContext.Response.RegisterForDispose(scopedDisposable);
            }

            return scopedDependency;
        }

        /// <summary>
        ///     Register disposable dependencies for a middleware.
        /// </summary>
        private void RegisterScopedDependencyForDispose(IMiddleware middleware, params IDependency[] scopedDependencies)
        {
            var disposableDependencies =
                scopedDependencies
                    .Where(d => d is IDisposable)
                    .Cast<IDisposable>()
                    .ToList();
            var disposables =
                _disposableScopedMiddlewareDependencies.GetOrAdd(
                    middleware,
                    new List<IDisposable>(disposableDependencies.Count));
            foreach (var disposable in disposableDependencies)
            {
                disposables.Add(disposable);
            }
        }

        /// <summary>
        ///     We should not be able to re-use a disposed instance of this class. All the singletons are already
        ///     disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void AssertNotDisposed()
        {
            if (_isCompositionRootDisposed)
            {
                throw new ObjectDisposedException(ToString());
            }
        }
    }
}