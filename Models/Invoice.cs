using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Index("InvoiceNumber", Name = "UQ__Invoices__D776E981AF49C804", IsUnique = true)]
public partial class Invoice
{
    [Key]
    [Column("InvoiceID")]
    public int InvoiceId { get; set; }

    [Column("JobOrderID")]
    public int JobOrderId { get; set; }

    [Column("CreatedByUserID")]
    public int? CreatedByUserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string InvoiceNumber { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? InvoiceDate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? SubTotal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? TaxAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal GrandTotal { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? InvoiceStatus { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? PaymentStatus { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Invoices")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("JobOrderId")]
    [InverseProperty("Invoices")]
    public virtual JobOrder JobOrder { get; set; } = null!;

    [InverseProperty("Invoice")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
