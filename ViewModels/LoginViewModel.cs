using System.ComponentModel.DataAnnotations;

namespace AutoRepairERD.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // For multi-role selection at login
        public int? SelectedRoleId { get; set; }
        public System.Collections.Generic.List<System.Tuple<int, string>> Roles { get; set; } = new System.Collections.Generic.List<System.Tuple<int, string>>();
    }
}