using System.IO;
using System.Threading.Tasks;
using AspNetCorePureDiApi.Models;
using Microsoft.AspNetCore.Http;

namespace AspNetCorePureDiApi.Middlewares
{
    public class ConventionalMiddleware
    {
        private readonly RequestDelegate _next;

        public ConventionalMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }
        
        public async Task InvokeAsync(HttpContext context, IDependency dependency)
        {
            await _next.Invoke(context);
            await using var streamWriter = new StreamWriter(context.Response.Body, leaveOpen: true);
            await streamWriter.WriteLineAsync($" HELLO FROM MIDDLEWARE {dependency}");
        }
    }
}