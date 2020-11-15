using System.Diagnostics.CodeAnalysis;
using AspNetCorePureDiApi;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class IntegrationTestsFactory :
        WebApplicationFactory<Startup>
    {
    }
}