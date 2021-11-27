using System.Collections.Generic;

namespace App.Web.Endpoints.ProjectEndpoints {
    public class ListIncompleteResponse {
        public int ProjectId { get; set; }
        public List<ToDoItemRecord> IncompleteItems { get; set; }
    }
}
