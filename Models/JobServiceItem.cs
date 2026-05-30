using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class JobServiceItem
{
    [Key]
    [Column("JobServiceItemID")]
    public int JobServiceItemId { get; set; }

    [Column("JobOrderID")]
    public int JobOrderId { get; set; }

    [Column("ServiceID")]
    public int ServiceId { get; set; }

    [Column("MechanicID")]
    public int? MechanicId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? HoursWorked { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? HourlyRate { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? ServicePrice { get; set; }

    [Unicode(false)]
    public string? Notes { get; set; }

    [ForeignKey("JobOrderId")]
    [InverseProperty("JobServiceItems")]
    public virtual JobOrder JobOrder { get; set; } = null!;

    [ForeignKey("MechanicId")]
    [InverseProperty("JobServiceItems")]
    public virtual Employee? Mechanic { get; set; }

    [ForeignKey("ServiceId")]
    [InverseProperty("JobServiceItems")]
    public virtual Service Service { get; set; } = null!;
}
