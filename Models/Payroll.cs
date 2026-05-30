using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class Payroll
{
    [Key]
    [Column("PayrollID")]
    public int PayrollId { get; set; }

    [Column("EmployeeID")]
    public int EmployeeId { get; set; }

    public int? PayrollMonth { get; set; }

    public int? PayrollYear { get; set; }

    public int? TotalWorkingDays { get; set; }

    public int? TotalPresentDays { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? OvertimeHours { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? BonusAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? DeductionAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? GrossSalary { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? NetSalary { get; set; }

    public DateOnly? PaymentDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PayrollStatus { get; set; }

    [ForeignKey("EmployeeId")]
    [InverseProperty("Payrolls")]
    public virtual Employee Employee { get; set; } = null!;

    [InverseProperty("Payroll")]
    public virtual ICollection<SalaryAdjustment> SalaryAdjustments { get; set; } = new List<SalaryAdjustment>();
}
