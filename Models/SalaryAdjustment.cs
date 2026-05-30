using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class SalaryAdjustment
{
    [Key]
    [Column("AdjustmentID")]
    public int AdjustmentId { get; set; }

    [Column("PayrollID")]
    public int PayrollId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? AdjustmentType { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Amount { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Reason { get; set; }

    [ForeignKey("PayrollId")]
    [InverseProperty("SalaryAdjustments")]
    public virtual Payroll Payroll { get; set; } = null!;
}
