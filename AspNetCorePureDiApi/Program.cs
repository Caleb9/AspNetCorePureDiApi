using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AspNetCorePureDiApi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            /* Turns out this type of configuration of Host is needed to enable integration tests to overwrite
             * dependency registrations. I.e. we need to use Host (not WebHost) and explicitly call UseStartup.
             * This makes ConfigureServices in IntegrationTestsFactory execute AFTER ConfigureServices in Startup. */
            return Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());
        }
    }
}