using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Index("JobNumber", Name = "UQ__JobOrder__A6B9B88202D22806", IsUnique = true)]
public partial class JobOrder
{
    [Key]
    [Column("JobOrderID")]
    public int JobOrderId { get; set; }

    [Column("CustomerID")]
    public int CustomerId { get; set; }

    [Column("VehicleID")]
    public int VehicleId { get; set; }

    [Column("AdvisorID")]
    public int? AdvisorId { get; set; }

    [Column("MechanicID")]
    public int? MechanicId { get; set; }

    [Column("CreatedByUserID")]
    public int? CreatedByUserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string JobNumber { get; set; } = null!;

    [Unicode(false)]
    public string ComplaintDescription { get; set; } = null!;

    [Unicode(false)]
    public string? DiagnosisNotes { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? EstimatedCompletionDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? StartDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CompletionDate { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? FinalCost { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("AdvisorId")]
    [InverseProperty("JobOrderAdvisors")]
    public virtual Employee? Advisor { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("JobOrders")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("JobOrders")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("JobOrder")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("JobOrder")]
    public virtual ICollection<JobPartItem> JobPartItems { get; set; } = new List<JobPartItem>();

    [InverseProperty("JobOrder")]
    public virtual ICollection<JobServiceItem> JobServiceItems { get; set; } = new List<JobServiceItem>();

    [ForeignKey("MechanicId")]
    [InverseProperty("JobOrderMechanics")]
    public virtual Employee? Mechanic { get; set; }

    [ForeignKey("VehicleId")]
    [InverseProperty("JobOrders")]
    public virtual Vehicle Vehicle { get; set; } = null!;
}
