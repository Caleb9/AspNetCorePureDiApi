using System;
using System.Collections.Generic;
using AspNetCorePureDiApi.Controllers;
using AspNetCorePureDiApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AspNetCorePureDiApi.DependencyRoot
{
    /// <summary>
    ///     An example of Pure DI composition root for Controllers.
    ///     Creates instances of Controllers with their dependencies.
    ///     Holds references to Controllers' singleton dependencies. If any of them implement IDisposable, then they
    ///     need to get disposed when the application shuts down. That's why ControllerActivator implements
    ///     IDisposable itself. This is just an example, but in a real life project the responsibility of storing
    ///     the singleton dependencies should be delegated to another class probably.
    /// </summary>
    /// <remarks>
    ///     Possibly this is an overkill as when the application shuts down all the memory gets reclaimed either way.
    ///     However it's a good practice to always dispose of everything that is IDisposable
    /// </remarks>
    public sealed class ControllerActivator
        : IControllerActivator, IDisposable
    {
        /// <summary>
        ///     Ensures that this instance along with associated disposables can be disposed by <see cref="Program" />
        ///     when the application is shutdown. Just by registering ControllerActivator in Startup's
        ///     IServiceCollection does not dispose of it automatically.
        ///     The field is internal so it is not used outside of this project. Integration tests should be able to
        ///     construct their own copy of ControllerActivator, probably with IDisposable dependencies swapped with
        ///     fakes (as it indicates that they access out-of-process resources). Such scenario would call for more
        ///     complex solution where those dependencies are accessible for overwriting in tests.
        /// </summary>
        internal static readonly ControllerActivator Singleton = new ControllerActivator();

        /// <summary>
        ///     Singleton dependencies that should be disposed on application shutdown are added to this list.
        /// </summary>
        private readonly List<IDisposable> _singletonDisposables = new List<IDisposable>();

        /// <summary>
        ///     An example of a singleton, disposable object used in controller's dependency graph.
        /// </summary>
        private readonly DisposableDependency _singletonDisposableDependency;
        
        public ControllerActivator()
        {
            _singletonDisposableDependency = RegisterSingletonForDispose(new DisposableDependency());
        }

        object IControllerActivator.Create(ControllerContext context)
        {
            if (GetControllerType(context) == typeof(HelloController))
            {
                var scopedDependency = RegisterForDispose(context, new DisposableDependency());
                return new HelloController(
                    _singletonDisposableDependency,
                    scopedDependency);
            }

            throw new InvalidOperationException("Unknown Controller Type");
        }

        void IControllerActivator.Release(ControllerContext context, object controller)
        {
        }

        public void Dispose()
        {
            foreach (var disposable in _singletonDisposables)
            {
                disposable.Dispose();
            }
        }

        private T RegisterSingletonForDispose<T>(T disposableSingleton)
            where T : IDisposable
        {
            _singletonDisposables.Add(disposableSingleton);
            return disposableSingleton;
        }

        private Type GetControllerType(ControllerContext context)
        {
            return context.ActionDescriptor.ControllerTypeInfo.AsType();
        }

        /// <summary>
        ///    <paramref name="scopedDisposable"/> will be disposed when request handling is finished. 
        /// </summary>
        private T RegisterForDispose<T>(ActionContext context, T scopedDisposable)
            where T : IDisposable
        {
            context.HttpContext.Response.RegisterForDispose(scopedDisposable);
            return scopedDisposable;
        }
    }
}