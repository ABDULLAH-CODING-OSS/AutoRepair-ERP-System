namespace AutoRepairERD.Models
{
    public class RoleListItem
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AssignedCount { get; set; }
    }
}
