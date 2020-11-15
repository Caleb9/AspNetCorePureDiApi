using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using AspNetCorePureDiApi;
using AspNetCorePureDiApi.PureDi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class IntegrationTestsFactory :
        WebApplicationFactory<Startup>
    {
        internal HttpClient CreateClient(
            CompositionRoot testingCompositionRoot)
        {
            return WithWebHostBuilder(builder =>
                    builder.ConfigureServices(services =>
                        /* Replace CompositionRoot registered in Startup with testingCompositionRoot, where some
                         * dependencies can be substituted with test doubles. */
                        services.AddSingleton(testingCompositionRoot)))
                .CreateDefaultClient();
        }
    }
}