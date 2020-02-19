# ASP.NET Core 3.1 Pure DI Example

A simplistic example of utilizing Pure DI in ASP.NET Core 3.1 Web API
project.

The __Composition Root__ is implemented in a custom
IControllerActivator and IMiddlewareFactory. Main problem solved here
is how to dispose of IDisposable dependencies, including objects with
both _singleton_ and _request-scoped_ lifestyle.

### Usage

Try to build and run the application in console, e.g. on Linux with
`Debug | Any CPU` configuration:

```
$ dotnet build
$ ./AspNetCorePureDiApi/bin/Debug/netcoreapp3.1/AspNetCorePureDiApi
```

Open a web browser, and navigate to
`http://localhost:5000/api/hello`. Refresh the page several times. In
the console, observe how scoped dependencies get disposed after each
request. The controller response includes ids of its dependencies (one
singleton and one scoped). A middleware then adds some extra
information to the response, reporting its own dependencies.

When finished, press `Ctrl+C` in the console to shut down the
application to see that singleton dependencies get disposed as well.


### Implementation

Implementation replaces the default ControllerActivator and
MiddlewareFactory with a custom class which serves as the Composition
Root. Crucial here is the fact that `CompositionRoot` exposes a
singleton instance that gets disposed when application shuts
down. This in turn, disposes all of the singleton dependencies held by
the `CompositionRoot`. Scoped controller dependencies are registered
for disposal in `ControllerContext.HttpContext.Response` and get
automatically disposed by the framework along with the
response. Scoped middleware dependencies are disposed in a similar
manner. `CompositionRoot` implements a Singleton pattern itself, so it
is accessible from the `Program` for disposal. Simply registering a
singleton IDisposable service in
`Startup.ConfigureServices(IServiceCollection services)` method does
not seem to dispose of it when application shuts down. Alternatively,
we could possibly pass an instance of `CompositionRoot` from `Program`
to `Startup`.

Note that despite the fact that all the DI logic is implemented in the
`CompositionRoot`, I would probably delegate the responsibility of
creating dependency graphs to another class or classes in a real life
project.

See comments in the code for more information.


#### Request-Scoped Dependencies Issue

With the current implementation, when using IMiddlewareFactory to
create request-scoped middleware, there are two scopes created per web
request: one for middleware's dependencies and one for
controller's. I.e. if for example both UserMiddleware and
UserController depend on IUserRepository, and IUserRepository should
have request scoped lifetime, then there will be two separate
instances of IUserRepository created.

It is potentially possible to workaround this issue by using
`AsyncLocal<IDictionary<Type, object>>` field that gets populated with
dependencies in `IMiddlewareFactory.Create` method and consumed by
`IControllerActivator.Create`. However we are loosing compile-time
checking of dependencies in the latter method (when creating a
controller), which is the whole point of Pure DI.

A compromise could be to only allow singleton middlewares, as in
classic Microsoft.Owin pipeline but that of course poses other
limitations.


### Discussion

It is entirely possible that there is a better way to do this, or that
such implementation has potential problems which I don't see. Comments
are welcome in the Issues section.
