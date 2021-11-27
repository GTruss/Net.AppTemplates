using App.Web;

using RestSharp;

using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

namespace App.FunctionalTests.ControllerApis {
    [Collection("Sequential")]
    public class MetaControllerInfo : IClassFixture<CustomWebApplicationFactory<Startup>> {
        private readonly HttpClient _client;
        private readonly string root = "http://localhost:57678/";

        public MetaControllerInfo(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReturnsVersionAndLastUpdateDate() {
            var request = new RestRequest(Method.GET);
            var client = new RestClient(root + "/info");
            var response = await client.ExecuteAsync(request);

            //var response = await _client.GetAsync("/info");
            //response.EnsureSuccessStatusCode();
            //var stringResponse = await response.Content.ReadAsStringAsync();

            Assert.Contains("Version", response.Content);
            Assert.Contains("Last Updated", response.Content);
        }
    }
}
