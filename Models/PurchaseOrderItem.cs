using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class PurchaseOrderItem
{
    [Key]
    [Column("POItemID")]
    public int PoitemId { get; set; }

    [Column("PurchaseOrderID")]
    public int PurchaseOrderId { get; set; }

    [Column("PartID")]
    [Display(Name = "Part")]
    public int PartId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Unit cost must be greater than zero.")]
    [Display(Name = "Unit Cost")]
    public decimal? UnitCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total cost must be greater than zero.")]
    [Display(Name = "Line Total")]
    public decimal? TotalCost { get; set; }

    [ForeignKey("PartId")]
    [InverseProperty("PurchaseOrderItems")]
    public virtual Part Part { get; set; } = null!;

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("PurchaseOrderItems")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
