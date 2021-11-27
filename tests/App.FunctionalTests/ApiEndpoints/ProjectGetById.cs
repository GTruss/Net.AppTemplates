using Ardalis.HttpClientTestExtensions;

using App.Web;
using App.Web.Endpoints.ProjectEndpoints;

using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using RestSharp;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace App.FunctionalTests.ApiEndpoints {
    [Collection("Sequential")]
    public class ProjectGetById : IClassFixture<CustomWebApplicationFactory<Startup>> {
        private readonly HttpClient _client;
        private readonly string root = "http://localhost:57678/";

        public ProjectGetById(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReturnsSeedProjectGivenId1() {

            var request = new RestRequest(Method.GET);
            var client = new RestClient(root + GetProjectByIdRequest.BuildRoute(1));
            var response = await client.ExecuteAsync(request);

            var result = JsonConvert.DeserializeObject<GetProjectByIdResponse>(response.Content);
            //var result = await _client.GetAndDeserialize<GetProjectByIdResponse>(GetProjectByIdRequest.BuildRoute(1));

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(SeedData.TestProject1.Name, result.Name);
            Assert.Equal(3, result.Items.Count);
        }

        [Fact]
        public async Task ReturnsNotFoundGivenId0() {
            string route = GetProjectByIdRequest.BuildRoute(0);
            _ = await _client.GetAndEnsureNotFound(route);
        }
    }
}
