using App.Web;

using RestSharp;

using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

namespace App.FunctionalTests.ControllerViews {
    [Collection("Sequential")]
    public class HomeControllerIndex : IClassFixture<CustomWebApplicationFactory<Startup>> {
        private readonly HttpClient _client;
        private readonly string root = "http://localhost:57678/";

        public HomeControllerIndex(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReturnsViewWithCorrectMessage() {
            //HttpResponseMessage response = await _client.GetAsync("https://google.com");

            var request = new RestRequest(Method.GET);
            var client = new RestClient(root);
            IRestResponse response = await client.ExecuteAsync(request);


            //response.EnsureSuccessStatusCode();
            //string stringResponse = await response.Content.ReadAsStringAsync();

            //Assert.Contains("App.Web", stringResponse);
            Assert.Contains("App.Web", response.Content);
        }
    }
}
