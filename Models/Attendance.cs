using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Table("Attendance")]
public partial class Attendance
{
    [Key]
    [Column("AttendanceID")]
    public int AttendanceId { get; set; }

    [Column("EmployeeID")]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Attendance Date")]
    public DateOnly? AttendanceDate { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Check In Time")]
    public TimeOnly? CheckInTime { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Check Out Time")]
    public TimeOnly? CheckOutTime { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0, 24)]
    [Display(Name = "Overtime Hours")]
    public decimal? OvertimeHours { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    [Display(Name = "Status")]
    public string? Status { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Attendances")]
    public virtual Employee Employee { get; set; } = null!;
}
