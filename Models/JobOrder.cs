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
    [Display(Name = "Customer")]
    [Required]
    public int CustomerId { get; set; }

    [Column("VehicleID")]
    [Display(Name = "Vehicle")]
    [Required]
    public int VehicleId { get; set; }

    [Column("AdvisorID")]
    [Display(Name = "Service Advisor")]
    public int? AdvisorId { get; set; }

    [Column("MechanicID")]
    [Display(Name = "Mechanic")]
    public int? MechanicId { get; set; }

    [Column("CreatedByUserID")]
    [Display(Name = "Created By")]
    public int? CreatedByUserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string JobNumber { get; set; } = null!;

    [Unicode(false)]
    [Display(Name = "Complaint")]
    [Required]
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
    [Display(Name = "Status")]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Estimated Cost")]
    [Range(0, (double)decimal.MaxValue)]
    public decimal? EstimatedCost { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    [Display(Name = "Final Cost")]
    [Range(0, (double)decimal.MaxValue)]
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
