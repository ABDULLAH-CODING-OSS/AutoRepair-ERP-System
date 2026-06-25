using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class Service
{
    [Key]
    [Column("ServiceID")]
    public int ServiceId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string ServiceName { get; set; } = null!;

    [Unicode(false)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? StandardHours { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? FixedPrice { get; set; }

    public bool IsActive { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<JobServiceItem> JobServiceItems { get; set; } = new List<JobServiceItem>();
}
