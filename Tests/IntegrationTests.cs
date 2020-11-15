using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class IntegrationTests :
        IClassFixture<IntegrationTestsFactory>
    {
        private readonly IntegrationTestsFactory _factory;

        public IntegrationTests(
            IntegrationTestsFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Hello_responds_with_Ok()
        {
            using var client = _factory.CreateDefaultClient();

            var response = await client.GetAsync("/api/hello");
            
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}