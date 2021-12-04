﻿using Ardalis.HttpClientTestExtensions;

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

        public ProjectCreate(CustomWebApplicationFactory<Startup> factory) {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReturnsOneProject() {
            var result = await _client.GetAndDeserialize<IEnumerable<ProjectDTO>>("/api/projects");

            Assert.Single(result);
            Assert.Contains(result, i => i.Name == SeedData.TestProject1.Name);
        }
    }
}