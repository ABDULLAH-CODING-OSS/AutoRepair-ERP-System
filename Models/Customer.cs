using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class Customer
{
    [Key]
    [Column("CustomerID")]
    public int CustomerId { get; set; }

    [Column("CreatedByUserID")]
    public int? CreatedByUserId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string FirstName { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? LastName { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Phone { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Address { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? City { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Customers")]
    public virtual User? CreatedByUser { get; set; }

    [InverseProperty("Customer")]
    public virtual ICollection<JobOrder> JobOrders { get; set; } = new List<JobOrder>();

    [InverseProperty("Customer")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
