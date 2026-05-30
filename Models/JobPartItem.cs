using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class JobPartItem
{
    [Key]
    [Column("JobPartItemID")]
    public int JobPartItemId { get; set; }

    [Column("JobOrderID")]
    public int JobOrderId { get; set; }

    [Column("PartID")]
    public int PartId { get; set; }

    public int? Quantity { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? UnitPrice { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? TotalPrice { get; set; }

    [ForeignKey("JobOrderId")]
    [InverseProperty("JobPartItems")]
    public virtual JobOrder JobOrder { get; set; } = null!;

    [ForeignKey("PartId")]
    [InverseProperty("JobPartItems")]
    public virtual Part Part { get; set; } = null!;
}
