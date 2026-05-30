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
    public int UserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? EmployeeCode { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? LastName { get; set; }

    [Column("CNIC")]
    [StringLength(20)]
    [Unicode(false)]
    public string? Cnic { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Address { get; set; }

    public DateOnly? HireDate { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? Designation { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? BasicSalary { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? HourlyRate { get; set; }

    public bool? IsActive { get; set; }

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
    public virtual User User { get; set; } = null!;
}
