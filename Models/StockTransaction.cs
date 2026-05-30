using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class StockTransaction
{
    [Key]
    [Column("TransactionID")]
    public int TransactionId { get; set; }

    [Column("PartID")]
    public int PartId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? TransactionType { get; set; }

    public int? Quantity { get; set; }

    public int? PreviousStock { get; set; }

    public int? NewStock { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? ReferenceNumber { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Remarks { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? TransactionDate { get; set; }

    [ForeignKey("PartId")]
    [InverseProperty("StockTransactions")]
    public virtual Part Part { get; set; } = null!;
}
