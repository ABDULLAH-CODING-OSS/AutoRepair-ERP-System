using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class LowStockAlert
{
    [Key]
    [Column("AlertID")]
    public int AlertId { get; set; }

    [Column("PartID")]
    public int PartId { get; set; }

    public int? CurrentQuantity { get; set; }

    public int? ReorderLevel { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? AlertDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Status { get; set; }

    [ForeignKey("PartId")]
    [InverseProperty("LowStockAlerts")]
    public virtual Part Part { get; set; } = null!;
}
