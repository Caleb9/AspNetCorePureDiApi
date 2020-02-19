using System.Diagnostics.CodeAnalysis;
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
                /* Replace default MiddlewareFactory with custom one implementing Pure DI. See comments in
                 * CompositionRoot for explanation why this is implementing the Singleton pattern. */
                .AddSingleton<IMiddlewareFactory>(CompositionRoot.Singleton)
                /* Replace default ControllerActivator with custom one implementing Pure DI. */
                .AddSingleton<IControllerActivator>(CompositionRoot.Singleton)
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

            app.UseMiddleware<Middlewares.MyMiddleware>();
            app.UseRouting();

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}