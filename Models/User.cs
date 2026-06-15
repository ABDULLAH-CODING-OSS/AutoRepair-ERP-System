using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Index("Username", Name = "UQ__Users__536C85E47D4D5C4C", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D105344D029389", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters.")]
    [Unicode(false)]
    [Display(Name = "Username")]
    public string Username { get; set; } = null!;

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    [Unicode(false)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Required]
    [StringLength(255)]
    [Unicode(false)]
    [Display(Name = "Password")]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    [Display(Name = "Full Name")]
    public string? FullName { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string? Phone { get; set; }

    public bool? IsActive { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    [InverseProperty("User")]
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<JobOrder> JobOrders { get; set; } = new List<JobOrder>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    //[InverseProperty("CreatedByUser")]
    //public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();

    [InverseProperty("User")]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
