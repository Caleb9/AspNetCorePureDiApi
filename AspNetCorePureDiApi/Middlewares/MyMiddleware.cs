using System.IO;
using System.Threading.Tasks;
using AspNetCorePureDiApi.Models;
using Microsoft.AspNetCore.Http;

namespace AspNetCorePureDiApi.Middlewares
{
    public class MyMiddleware : IMiddleware
    {
        private readonly IDependency _scopedDependency;
        private readonly IDependency _singletonDependency;

        public MyMiddleware(
            IDependency singletonDependency,
            IDependency scopedDependency)
        {
            _singletonDependency = singletonDependency;
            _scopedDependency = scopedDependency;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await next.Invoke(context);

            await using var bodyWriter = new StreamWriter(context.Response.Body, leaveOpen: true);
            await bodyWriter.WriteLineAsync(
                $" Also, hello from middleware with {_singletonDependency} and {_scopedDependency}!");
        }
    }
}