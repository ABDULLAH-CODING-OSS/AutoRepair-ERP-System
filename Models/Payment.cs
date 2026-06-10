using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class Payment
{
    [Key]
    [Column("PaymentID")]
    public int PaymentId { get; set; }

    [Column("InvoiceID")]
    [Display(Name = "Invoice")]
    public int InvoiceId { get; set; }

    [Column(TypeName = "datetime")]
    [Display(Name = "Payment Date")]
    [DataType(DataType.DateTime)]
    public DateTime? PaymentDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    [Display(Name = "Payment Method")]
    public string? PaymentMethod { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TransactionReference { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Notes { get; set; }

    [ForeignKey("InvoiceId")]
    [InverseProperty("Payments")]
    public virtual Invoice Invoice { get; set; } = null!;
}
