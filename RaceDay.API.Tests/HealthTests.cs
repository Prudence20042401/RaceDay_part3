using System.Net;
using Xunit;

namespace RaceDay.API.Tests
{
    public class HealthTests : ApiTestBase
    {
        public HealthTests(CustomWebApplicationFactory factory) : base(factory) { }

        [Fact]
        public async Task Health_ReturnsOk()
        {
            var response = await Client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}