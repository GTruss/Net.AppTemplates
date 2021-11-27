using App.Core;
using App.Core.ProjectAggregate;
using App.Core.ProjectAggregate.Specifications;
using App.SharedKernel.Interfaces;
using App.Web.ApiModels;
using App.Web.ViewModels;

using Microsoft.AspNetCore.Mvc;

using System.Linq;
using System.Threading.Tasks;

namespace App.Web.Controllers {
    [Route("[controller]")]
    public class ProjectController : Controller {
        private readonly IRepository<Project> _projectRepository;

        public ProjectController(IRepository<Project> projectRepository) {
            _projectRepository = projectRepository;
        }

        // GET project/{projectId?}
        [HttpGet("{projectId:int}")]
        public async Task<IActionResult> Index(int projectId = 1) {
            var spec = new ProjectByIdWithItemsSpec(projectId);
            var project = await _projectRepository.GetBySpecAsync(spec);

            var dto = new ProjectViewModel {
                Id = project.Id,
                Name = project.Name,
                Items = project.Items
                            .Select(item => ToDoItemViewModel.FromToDoItem(item))
                            .ToList()
            };
            return View(dto);
        }
    }
}
