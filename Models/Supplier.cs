using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class Supplier
{
    [Key]
    [Column("SupplierID")]
    public int SupplierId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string CompanyName { get; set; } = null!;

    // Alias used by views
    public string? SupplierName => CompanyName;

    [StringLength(100)]
    [Unicode(false)]
    public string? ContactPerson { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Address { get; set; }

    [InverseProperty("Supplier")]
    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();

    [InverseProperty("Supplier")]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
