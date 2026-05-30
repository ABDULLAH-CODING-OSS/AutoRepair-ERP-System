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
    public int PartId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? UnitCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? TotalCost { get; set; }

    [ForeignKey("PartId")]
    [InverseProperty("PurchaseOrderItems")]
    public virtual Part Part { get; set; } = null!;

    [ForeignKey("PurchaseOrderId")]
    [InverseProperty("PurchaseOrderItems")]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
