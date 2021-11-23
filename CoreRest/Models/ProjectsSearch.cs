using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreRest.Models
{    


    public class ProjectsSearch
    {
        public int total_count { get; set; }
        public bool incomplete_results { get; set; }
        public List<ProjectModel> items { get; set; }
    }


}
