using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AutoRepairERD.Models;

public partial class SalaryAdjustment
{
    [Key]
    [Column("AdjustmentID")]
    public int AdjustmentId { get; set; }

    [Column("PayrollID")]
    [Required(ErrorMessage = "Payroll is required")]
    public int PayrollId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    [Required(ErrorMessage = "Adjustment Type is required")]
    public string? AdjustmentType { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Reason { get; set; }

    //[ForeignKey("PayrollId")]
    //[InverseProperty("SalaryAdjustments")]

    //public virtual Payroll Payroll { get; set; } = null!;
    [ForeignKey(nameof(PayrollId))]
    [InverseProperty(nameof(Payroll.SalaryAdjustments))]
    [ValidateNever]
    public virtual Payroll? Payroll { get; set; }

}
