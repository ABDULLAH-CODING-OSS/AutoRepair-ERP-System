using System;
using System.Linq;
using AutoRepairERD.Models;

namespace AutoRepairERD.Services;

public static class LowStockAlertManager
{
    // Synchronize alert for a single part. Does not call SaveChanges.
    public static void SyncPart(ApplicationDbContext context, int partId)
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
            }
        }
    }

    // Synchronize alerts for all parts. Does not call SaveChanges.
    public static void SyncAll(ApplicationDbContext context)
    {
        var parts = context.Parts.ToList();
        foreach (var p in parts)
        {
            SyncPart(context, p.PartId);
        }
    }
}
