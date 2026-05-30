using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Index("Sku", Name = "UQ__Parts__CA1ECF0DF73028DB", IsUnique = true)]
public partial class Part
{
    [Key]
    [Column("PartID")]
    public int PartId { get; set; }

    [Column("CategoryID")]
    public int? CategoryId { get; set; }

    [Column("SupplierID")]
    public int? SupplierId { get; set; }

    [Column("SKU")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Sku { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string PartName { get; set; } = null!;

    [Unicode(false)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SalePrice { get; set; }

    public int? CurrentStock { get; set; }

    public int? ReorderLevel { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Unit { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? RackLocation { get; set; }

    public bool? IsActive { get; set; }

    [ForeignKey("CategoryId")]
    [InverseProperty("Parts")]
    public virtual Category? Category { get; set; }

    [InverseProperty("Part")]
    public virtual ICollection<JobPartItem> JobPartItems { get; set; } = new List<JobPartItem>();

    [InverseProperty("Part")]
    public virtual ICollection<LowStockAlert> LowStockAlerts { get; set; } = new List<LowStockAlert>();

    [InverseProperty("Part")]
    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    [InverseProperty("Part")]
    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Parts")]
    public virtual Supplier? Supplier { get; set; }
}
