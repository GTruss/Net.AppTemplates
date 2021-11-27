using Ardalis.HttpClientTestExtensions;

using App.Web;
using App.Web.ApiModels;

using Newtonsoft.Json;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using App.Web.Endpoints.ProjectEndpoints;
using RestSharp;

namespace App.FunctionalTests.ControllerApis {
    [Collection("Sequential")]
    public class ProjectCreate : IClassFixture<CustomWebApplicationFactory<Startup>> {
        private readonly HttpClient _client;
        private readonly string root = "http://localhost:57678/";

        public ProjectCreate(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReturnsOneProject() {
            var request = new RestRequest(Method.GET);
            var client = new RestClient(root + "/api/projects");
            var response = await client.ExecuteAsync(request);

            var result = JsonConvert.DeserializeObject<IEnumerable<ProjectDTO>>(response.Content);

            //var result = await _client.GetAndDeserialize<IEnumerable<ProjectDTO>>("/api/projects");

            Assert.Single(result);
            Assert.Contains(result, i => i.Name == SeedData.TestProject1.Name);
        }
    }
}
