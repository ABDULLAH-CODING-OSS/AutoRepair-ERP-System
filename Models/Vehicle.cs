using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

[Index("LicensePlate", Name = "UQ__Vehicles__026BC15CBFD16B8F", IsUnique = true)]
[Index("Vin", Name = "UQ__Vehicles__C5DF234C0DA3F9F8", IsUnique = true)]
public partial class Vehicle
{
    [Key]
    [Column("VehicleID")]
    public int VehicleId { get; set; }

    [Column("CustomerID")]
    public int CustomerId { get; set; }

    [Column("CreatedByUserID")]
    public int? CreatedByUserId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    [Required]
    public string LicensePlate { get; set; } = null!;

    [Column("VIN")]
    [StringLength(17)]
    [Unicode(false)]
    public string? Vin { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    [Required]
    public string Make { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    [Required]
    public string Model { get; set; } = null!;

    public int? ManufacturingYear { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Color { get; set; }

    [Range(0, int.MaxValue)]
    public int? Mileage { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? EngineNumber { get; set; }

    [Unicode(false)]
    public string? Notes { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Vehicles")]
    public virtual User? CreatedByUser { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Vehicles")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Vehicle")]
    public virtual ICollection<JobOrder> JobOrders { get; set; } = new List<JobOrder>();
}
