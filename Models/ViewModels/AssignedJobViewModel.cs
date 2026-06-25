using AutoRepairERD.Models;

namespace AutoRepairERD.Models.ViewModels
{
    public class AssignedJobViewModel
    {
        public required JobOrder Job { get; set; }
        public required string Priority { get; set; }
    }
}
