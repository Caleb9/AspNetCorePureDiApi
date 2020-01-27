# ASP.NET Core 3.1 Pure DI Example

A simplistic example of utilizing Pure DI in ASP.NET Core 3.1 Web API
project.

The __Composition Root__ is implemented in a custom
ControllerActivator. Main problem solved here is how to dispose of
IDisposable dependencies, including objects with both _singleton_ and
_request-scoped_ lifestyle.

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
singleton and one scoped).

When finished, press `Ctrl+C` in the console to shut down the
application to see that singleton dependencies get disposed as well.

### Implementation

Implementation replaces the default ControllerActivator with a custom
one which serves as the Composition Root. Crucial here is the fact
that `ControllerActivator` exposes a singleton instance that gets
disposed when application shuts down. This in turn, disposes all of
the singleton dependencies held by the `ControllerActivator`. Scoped
dependencies are registered for disposal in
`ControllerContext.HttpContext.Response` and get automatically
disposed by the framework along with the response.
`ControllerActivator` implements Singleton so it is accessible from
the `Program`. Simply registering a singleton IDisposable service in
`Startup.ConfigureServices(IServiceCollection services)` method does
not seem to dispose of it when application shuts down. Alternatively,
we could pass an instance of `ControllerActivator` from `Program` to
`Startup`.

Note that despite the fact that ControllerActivator is the Composition
Root, I would probably delegate the responsibility of creating
dependency graphs to another class or classes in a real life project.

See comments in the code for more information.

### Discussion

It is entirely possible that there is a better way to do this, or that
such implementation has potential problems which I don't see. Comments
are welcome in the Issues section.