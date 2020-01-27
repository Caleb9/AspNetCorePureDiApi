using System.Threading.Tasks;
using AspNetCorePureDiApi.DependencyRoot;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AspNetCorePureDiApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            try
            {
                await host.RunAsync();
            }
            finally
            {
                /* Dispose singletons held in ControllerActivator when application shuts down. */
                ControllerActivator.Singleton.Dispose();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
            
        }
    }
}