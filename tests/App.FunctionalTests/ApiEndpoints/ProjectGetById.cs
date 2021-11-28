using Ardalis.HttpClientTestExtensions;

using App.Web;
using App.Web.Endpoints.ProjectEndpoints;

using System.Net.Http;
using System.Threading.Tasks;

using Xunit;
using RestSharp;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace App.FunctionalTests.ApiEndpoints {
    [Collection("Sequential")]
    public class ProjectGetById : IClassFixture<CustomWebApplicationFactory<Startup>> {
        private readonly ILogger<ProjectGetById> _logger;
        private readonly HttpClient _client;

        public ProjectGetById(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
            _logger = (ILogger<ProjectGetById>)factory.Services.GetService(typeof(ILogger<ProjectGetById>));
        }

        [Fact]
        public async Task ReturnsSeedProjectGivenId1() {
            _logger.LogInformation("Path: {path}", GetProjectByIdRequest.BuildRoute(1));
            var result = await _client.GetAndDeserialize<GetProjectByIdResponse>(GetProjectByIdRequest.BuildRoute(1));

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
