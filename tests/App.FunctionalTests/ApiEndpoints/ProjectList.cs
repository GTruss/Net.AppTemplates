using Ardalis.HttpClientTestExtensions;

using App.Web;
using App.Web.Endpoints.ProjectEndpoints;

using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using Newtonsoft.Json;
using RestSharp;

namespace App.FunctionalTests.ApiEndpoints {
    [Collection("Sequential")]
    public class ProjectList : IClassFixture<CustomWebApplicationFactory<Startup>> {
        private readonly HttpClient _client;
        private readonly string root = "http://localhost:57678/";

        public ProjectList(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReturnsOneProject() {
            var request = new RestRequest(Method.GET);
            var client = new RestClient(root + "/Projects");
            var response = await client.ExecuteAsync(request);

            var result = JsonConvert.DeserializeObject<ProjectListResponse>(response.Content);

            //var result = await _client.GetAndDeserialize<ProjectListResponse>("/Projects");

            Assert.Single(result.Projects);
            Assert.Contains(result.Projects, i => i.Name == SeedData.TestProject1.Name);
        }
    }
}
