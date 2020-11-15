using System;
using System.Threading.Tasks;
using AspNetCorePureDiApi.Models;
using AspNetCorePureDiApi.PureDi;
using Moq;
using Xunit;

namespace Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Hello_responds_with_Ok()
        {
            using var factory = new IntegrationTestsFactory();
            using var client = factory.CreateDefaultClient();

            var response = await client.GetAsync("/api/hello");

            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task Disposable_singleton_dependencies_get_disposed()
        {
            var singletonDependencyStub = new Mock<IDependency>();
            var disposableMock = singletonDependencyStub.As<IDisposable>();

            using (var factory = new IntegrationTestsFactory())
            {
                using var client = factory.CreateClient(new CompositionRoot(singletonDependencyStub.Object));
                /* Apparently it is necessary to exercise the code in some way for the services to get disposed along
                 * with the factory. */
                await client.GetAsync("/api/hello");
            }

            disposableMock.Verify(m => m.Dispose());
        }

        [Fact]
        public async Task Disposable_scoped_dependencies_get_disposed()
        {
            using var factory = new IntegrationTestsFactory();
            var scopedDependencyStub = new Mock<IDependency>();
            var disposableMock = scopedDependencyStub.As<IDisposable>();
            using var client =
                factory.CreateClient(
                    new CompositionRoot(
                        scopedDependencyFactory: () => scopedDependencyStub.Object));

            await client.GetAsync("/api/hello");

            disposableMock.Verify(m => m.Dispose());
        }
    }
}