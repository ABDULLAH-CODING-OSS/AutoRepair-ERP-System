using AutoRepairERD.Models;

namespace AutoRepairERD.Models.ViewModels
{
    public class AssignedJobViewModel
    {
        public JobOrder Job { get; set; }
        public string Priority { get; set; }
    }
}
