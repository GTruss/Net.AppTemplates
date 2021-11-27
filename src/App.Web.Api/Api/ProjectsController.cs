using App.Core.ProjectAggregate;
using App.Core.ProjectAggregate.Specifications;
using App.SharedKernel.Interfaces;
using App.Web.Api.ApiModels;
using App.Web.Api.Models;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace App.Web.Api {
    /// <summary>
    /// A sample API Controller. Consider using API Endpoints (see Endpoints folder) for a more SOLID approach to building APIs
    /// https://github.com/ardalis/ApiEndpoints
    /// </summary>
    public class ProjectsController : BaseApiController {
        private readonly IRepository<Project> _repository;

        public ProjectsController(IRepository<Project> repository) {
            _repository = repository;
        }

        // GET: api/Projects
        [SwaggerOperation(
            Summary = "Gets a list of all Projects",
            Description = "Gets a list of all Projects",
            OperationId = "Projects.List",
            Tags = new[] { "Projects" })
        ]
        [HttpGet]
        public async Task<ActionResult<List<ProjectRecord>>> List() {
            var projects = (await _repository.ListAsync())
                .Select(project => new ProjectRecord(project.Id, project.Name))
                .ToList();

            return Ok(projects);
        }

        // GET: api/Projects/1
        [SwaggerOperation(
            Summary = "Gets a single Project",
            Description = "Gets a single Project by Id",
            OperationId = "Projects.GetById",
            Tags = new[] { "Projects" })
        ]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectDTO>> GetById(int id) {
            var projectSpec = new ProjectByIdWithItemsSpec(id);
            var project = await _repository.GetBySpecAsync(projectSpec);

            var result = new ProjectDTO {
                Id = project.Id,
                Name = project.Name,
                Items = new List<ToDoItemDTO>
                (
                    project.Items.Select(i => ToDoItemDTO.FromToDoItem(i)).ToList()
                )
            };

            return Ok(result);
        }

        // POST: api/Projects
        [SwaggerOperation(
            Summary = "Creates a new Project",
            Description = "Creates a new Project",
            OperationId = "Projects.Create",
            Tags = new[] { "Projects" })
        ]
        [HttpPost]
        public async Task<ActionResult<List<ProjectDTO>>> Post([FromBody] CreateProjectDTO request) {
            var newProject = new Project(request.Name);

            var createdProject = await _repository.AddAsync(newProject);

            var result = new ProjectDTO {
                Id = createdProject.Id,
                Name = createdProject.Name
            };
            return Ok(result);
        }

        // PATCH: api/Projects/{projectId}/complete/{itemId}
        [SwaggerOperation(
            Summary = "Completes Project",
            Description = "Completes a Project",
            OperationId = "Projects.Complete",
            Tags = new[] { "Projects" })
        ]
        [HttpPatch("{projectId:int}/complete/{itemId}")]
        public async Task<IActionResult> Complete(int projectId, int itemId) {
            var projectSpec = new ProjectByIdWithItemsSpec(projectId);
            var project = await _repository.GetBySpecAsync(projectSpec);
            if (project == null) return NotFound("No such project");

            var toDoItem = project.Items.FirstOrDefault(item => item.Id == itemId);
            if (toDoItem == null) return NotFound("No such item.");

            toDoItem.MarkComplete();
            await _repository.UpdateAsync(project);

            return Ok();
        }

        [HttpDelete]
        [SwaggerOperation(
            Summary = "Deletes a Project",
            Description = "Deletes a Project",
            OperationId = "Projects.Delete",
            Tags = new[] { "Projects" })
        ]
        public async Task<IActionResult> Delete(int projectId) {
            var aggregateToDelete = await _repository.GetByIdAsync(projectId); // TODO: pass cancellation token
            if (aggregateToDelete == null) return NotFound();

            await _repository.DeleteAsync(aggregateToDelete);

            return NoContent();
        }
    }
}
