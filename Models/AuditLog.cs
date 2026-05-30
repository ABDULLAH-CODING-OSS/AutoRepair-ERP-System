using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Models;

public partial class AuditLog
{
    [Key]
    [Column("AuditLogID")]
    public int AuditLogId { get; set; }

    [Column("UserID")]
    public int? UserId { get; set; }

    [StringLength(100)]
    [Unicode(false)]
    public string? TableName { get; set; }

    [Column("RecordID")]
    public int? RecordId { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? ActionType { get; set; }

    [Unicode(false)]
    public string? OldValues { get; set; }

    [Unicode(false)]
    public string? NewValues { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ActionDate { get; set; }

    [Column("IPAddress")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Ipaddress { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? User { get; set; }
}
