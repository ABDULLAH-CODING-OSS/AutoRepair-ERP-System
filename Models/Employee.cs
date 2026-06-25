using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Index("EmployeeCode", Name = "UQ__Employee__1F64254845D6CF98", IsUnique = true)]
public partial class Employee
{
    [Key]
    [Column("EmployeeID")]
    public int EmployeeId { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? EmployeeCode { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    [Required]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    [Required]
    public string? LastName { get; set; }

    [Column("CNIC")]
    [StringLength(20)]
    [Unicode(false)]
    [Required]
    [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC format should be 12345-1234567-1")]
    public string? Cnic { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    [Required]
    [RegularExpression(@"^0?3\d{9}$", ErrorMessage = "Phone format should be 03XXXXXXXXX")]
    public string? Phone { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Address { get; set; }

    [Required]
    public DateOnly? HireDate { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    [Required]
    public string? Designation { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Basic Salary must be greater than or equal to zero.")]
    public decimal? BasicSalary { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly Rate must be greater than or equal to zero.")]
    public decimal? HourlyRate { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Employee")]
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    [InverseProperty("Advisor")]
    public virtual ICollection<JobOrder> JobOrderAdvisors { get; set; } = new List<JobOrder>();

    [InverseProperty("Mechanic")]
    public virtual ICollection<JobOrder> JobOrderMechanics { get; set; } = new List<JobOrder>();

    [InverseProperty("Mechanic")]
    public virtual ICollection<JobServiceItem> JobServiceItems { get; set; } = new List<JobServiceItem>();

    [InverseProperty("Employee")]
    public virtual ICollection<Payroll> Payrolls { get; set; } = new List<Payroll>();

    [ForeignKey("UserId")]
    [InverseProperty("Employees")]
    public virtual User? User { get; set; }
}
