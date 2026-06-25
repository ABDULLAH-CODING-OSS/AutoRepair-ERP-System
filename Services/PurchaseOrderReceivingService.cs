using System;
using System.Linq;
using System.Threading.Tasks;
using AutoRepairERD.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Services
{
    public class ReceiveResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Handles receiving a Purchase Order from the dedicated "Receive" screen. This wraps the
    /// same stock-in / low-stock-alert-sync logic that PurchaseOrdersController.Edit already
    /// applies when a PO's status is changed to "Received" by hand, so both paths produce
    /// identical, non-duplicated StockTransaction rows.
    /// </summary>
    public class PurchaseOrderReceivingService
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderReceivingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ReceiveResult> ReceiveAsync(int purchaseOrderId)
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

            if (po == null) return new ReceiveResult { Success = false, Error = "Purchase order not found." };
            if (po.Status == "Received") return new ReceiveResult { Success = false, Error = "This purchase order has already been received." };
            if (po.Status == "Cancelled") return new ReceiveResult { Success = false, Error = "Cancelled purchase orders cannot be received." };
            if (po.PurchaseOrderItems == null || po.PurchaseOrderItems.Count == 0)
                return new ReceiveResult { Success = false, Error = "This purchase order has no line items." };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in po.PurchaseOrderItems)
                {
                    var qty = item.Quantity ?? 0;
                    if (qty <= 0) continue;

                    var part = await _context.Parts.FindAsync(item.PartId);
                    if (part == null) continue;

                    var refNumber = $"PO-{po.PurchaseOrderId}-{item.PoitemId}";

                    var alreadyReceived = await _context.StockTransactions.AnyAsync(s =>
                        s.ReferenceNumber == refNumber && s.PartId == item.PartId &&
                        s.Quantity == qty && s.TransactionType == "Stock In");
                    if (alreadyReceived) continue;

                    var previousStock = part.CurrentStock ?? 0;
                    var newStock = previousStock + qty;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        PartId = item.PartId,
                        TransactionType = "Stock In",
                        Quantity = qty,
                        PreviousStock = previousStock,
                        NewStock = newStock,
                        ReferenceNumber = refNumber,
                        TransactionDate = DateTime.Now,
                        Remarks = $"Purchase Order #{po.PurchaseOrderId} received from {po.Supplier?.CompanyName}"
                    });

                    part.CurrentStock = newStock;
                    _context.Parts.Update(part);
                    LowStockAlertManager.SyncPart(_context, part.PartId);
                }

                po.Status = "Received";
                _context.PurchaseOrders.Update(po);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ReceiveResult { Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new ReceiveResult { Success = false, Error = ex.Message };
            }
        }
    }
}
