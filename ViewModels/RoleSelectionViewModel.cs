using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoRepairERD.ViewModels
{
    public class RoleSelectionViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role.")]
        public int? SelectedRoleId { get; set; }

        public List<System.Tuple<int, string>> Roles { get; set; } = new List<System.Tuple<int, string>>();
    }
}
