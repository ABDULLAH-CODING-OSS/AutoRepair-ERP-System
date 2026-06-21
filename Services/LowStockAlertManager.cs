using System;
using System.Linq;
using AutoRepairERD.Models;

namespace AutoRepairERD.Services;

public static class LowStockAlertManager
{
    // Synchronize alert for a single part. Does not call SaveChanges.
    public static void SyncPart(ApplicationDbContext context, int partId, int? userId = null, string ip = null)
    {
        var part = context.Parts.Find(partId);
        if (part == null) return;

        var current = part.CurrentStock ?? 0;
        var reorder = part.ReorderLevel ?? 0;

        // Active alert = any alert not marked Resolved or Inactive
        var active = context.LowStockAlerts.FirstOrDefault(a => a.PartId == partId && a.Status != "Resolved" && a.Status != "Inactive");

        if (current <= reorder)
        {
            if (active == null)
            {
                var alert = new LowStockAlert
                {
                    PartId = partId,
                    CurrentQuantity = current,
                    ReorderLevel = reorder,
                    AlertDate = DateTime.Now,
                    Status = "Active"
                };
                context.LowStockAlerts.Add(alert);
                // Audit: Low Stock Alert Created (best-effort)
                try
                {
                    var partObj = context.Parts.Find(partId);
                    var audit = new AuditLog
                    {
                        UserId = userId,
                        TableName = "LowStockAlerts",
                        RecordId = 0,
                        ActionType = "Low Stock Alert Created",
                        OldValues = null,
                        NewValues = $"PartId={partId};PartName={(partObj!=null?partObj.PartName:"")};CurrentQuantity={current};ReorderLevel={reorder}",
                        ActionDate = DateTime.Now,
                        Ipaddress = ip
                    };
                    context.AuditLogs.Add(audit);
                }
                catch { }
            }
            else
            {
                // update information
                active.CurrentQuantity = current;
                active.ReorderLevel = reorder;
                active.AlertDate = active.AlertDate ?? DateTime.Now;
                context.LowStockAlerts.Update(active);
            }
        }
        else
        {
            if (active != null)
            {
                // Update snapshot of current stock when resolving
                active.CurrentQuantity = current;
                active.ReorderLevel = reorder;
                active.Status = "Resolved";
                active.AlertDate = DateTime.Now;
                context.LowStockAlerts.Update(active);
                // Audit: Low Stock Alert Resolved (best-effort)
                try
                {
                    var partObj = context.Parts.Find(partId);
                    var audit = new AuditLog
                    {
                        UserId = userId,
                        TableName = "LowStockAlerts",
                        RecordId = active.AlertId,
                        ActionType = "Low Stock Alert Resolved",
                        OldValues = $"PartId={partId};PartName={(partObj!=null?partObj.PartName:"")};PreviousQuantity={current}",
                        NewValues = $"PartId={partId};PartName={(partObj!=null?partObj.PartName:"")};CurrentQuantity={current};Status=Resolved",
                        ActionDate = DateTime.Now,
                        Ipaddress = ip
                    };
                    context.AuditLogs.Add(audit);
                }
                catch { }
            }
        }
    }

    // Synchronize alerts for all parts. Does not call SaveChanges.
    public static void SyncAll(ApplicationDbContext context, int? userId = null, string ip = null)
    {
        var parts = context.Parts.ToList();
        foreach (var p in parts)
        {
            SyncPart(context, p.PartId, userId, ip);
        }
    }
}
