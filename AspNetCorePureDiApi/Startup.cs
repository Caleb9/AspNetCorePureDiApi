using System.Diagnostics.CodeAnalysis;
using AspNetCorePureDiApi.Middlewares;
using AspNetCorePureDiApi.PureDi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCorePureDiApi
{
    public sealed class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(
            IServiceCollection services)
        {
            services
                /* We need to register the CompositionRoot as singleton to use it in factory method for both
                 * IMiddlewareFactory and IControllerActivator. Usage of factory method is necessary if we want to avoid
                 * creating two instances of CompositionRoot (one for IMiddlewareFactory and one for
                 * IControllerActivator). */
                .AddSingleton<CompositionRoot>()
                /* Replace default MiddlewareFactory with custom one implementing Pure DI. */
                .AddSingleton<IMiddlewareFactory>(s => s.GetRequiredService<CompositionRoot>())
                /* Replace default ControllerActivator with custom one implementing Pure DI. */
                .AddSingleton<IControllerActivator>(s => s.GetRequiredService<CompositionRoot>())
                /* Alternative implementation could do all the work of ControllerActivator in IControllerFactory */
                // .AddSingleton<IControllerFactory, ControllerFactory>()
                .AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMiddleware<MyMiddleware>();
            app.UseRouting();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}