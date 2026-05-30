using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class PurchaseOrder
{
    [Key]
    [Column("PurchaseOrderID")]
    public int PurchaseOrderId { get; set; }

    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [Column("CreatedByUserID")]
    public int? CreatedByUserId { get; set; }

    public DateOnly? OrderDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? TotalAmount { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("PurchaseOrders")]
    public virtual User? CreatedByUser { get; set; }

    [InverseProperty("PurchaseOrder")]
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    [ForeignKey("SupplierId")]
    [InverseProperty("PurchaseOrders")]
    public virtual Supplier Supplier { get; set; } = null!;
}
