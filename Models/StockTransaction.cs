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
    [Display(Name = "Part")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a part.")]
    public int PartId { get; set; }


    [StringLength(50)]
    [Unicode(false)]
    [Display(Name = "Transaction Type")]
    [Required(ErrorMessage = "Please select a transaction type.")]
    public string? TransactionType { get; set; }

    [Display(Name = "Quantity")]
    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
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
    [Display(Name = "Transaction Date")]
    [DisplayFormat(DataFormatString = "{0:dd-MMM-yyyy}", ApplyFormatInEditMode = true)]
    public DateTime? TransactionDate { get; set; }

    [ForeignKey("PartId")]
    [InverseProperty("StockTransactions")]
    public virtual Part Part { get; set; } = null!;

    // CreatedByUser removed: no CreatedByUserID column exists in the database for StockTransactions
}
