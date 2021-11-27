using App.Core.ProjectAggregate;

using System.Collections.Generic;

namespace App.Web.Endpoints.ProjectEndpoints {
    public class ProjectListResponse {
        public List<ProjectRecord> Projects { get; set; } = new();
    }
}
