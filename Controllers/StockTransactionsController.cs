using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using AutoRepairERD.Services;

[RoleAuthorize("Owner", "Admin", "Inventory Manager")]
public class StockTransactionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notifications;

    public StockTransactionsController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    // GET: STOCKTRANSACTIONS with search and filters
    public async Task<IActionResult> Index(string searchPart = "", string filterType = "", string startDate = "", string endDate = "")
    {
        var query = _context.StockTransactions
            .Include(s => s.Part)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchPart))
            query = query.Where(s => s.Part.PartName.Contains(searchPart));

        if (!string.IsNullOrWhiteSpace(filterType))
            query = query.Where(s => s.TransactionType == filterType);

        if (DateTime.TryParse(startDate, out var start))
            query = query.Where(s => s.TransactionDate >= start);
        if (DateTime.TryParse(endDate, out var end))
        {
            var endOfDay = end.AddDays(1);
            query = query.Where(s => s.TransactionDate < endOfDay);
        }

        var transactions = await query.OrderByDescending(s => s.TransactionDate).ToListAsync();

        ViewBag.SearchPart = searchPart;
        ViewBag.FilterType = filterType;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.TransactionTypes = new[] { "", "Stock In", "Stock Out", "Adjustment" };
        ViewBag.SelectedFilterType = filterType;

        return View(transactions);
    }

    // GET: STOCKTRANSACTIONS/Details/5
    public async Task<IActionResult> Details(int? transactionid)
    {
        if (transactionid == null) return NotFound();

        var stocktransaction = await _context.StockTransactions
            .Include(s => s.Part)
                .ThenInclude(p => p.Supplier)
            .Include(s => s.Part.Category)
            .FirstOrDefaultAsync(m => m.TransactionId == transactionid);

        if (stocktransaction == null) return NotFound();

        return View(stocktransaction);
    }

    // GET: STOCKTRANSACTIONS/Create
    public IActionResult Create()
    {
        ViewBag.Parts = new SelectList(_context.Parts.Where(p => p.IsActive == true).OrderBy(p => p.PartName).ToList(), "PartId", "PartName");
        ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
        return View();
    }

    // POST: STOCKTRANSACTIONS/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PartId,TransactionType,Quantity,Remarks")] StockTransaction stocktransaction)
    {
        ModelState.Remove("Part");
        ModelState.Remove("TransactionDate");
        ModelState.Remove("PreviousStock");
        ModelState.Remove("NewStock");
        ModelState.Remove("ReferenceNumber");

        if (stocktransaction.PartId <= 0)
            ModelState.AddModelError("PartId", "Please select a valid part.");
        if (string.IsNullOrWhiteSpace(stocktransaction.TransactionType))
            ModelState.AddModelError("TransactionType", "Please select a transaction type.");
        if (stocktransaction.Quantity == null || stocktransaction.Quantity <= 0)
            ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");

        if (!ModelState.IsValid)
        {
            ViewBag.Parts = new SelectList(_context.Parts.OrderBy(p => p.PartName), "PartId", "PartName", stocktransaction.PartId);
            ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
            return View(stocktransaction);
        }

        var part = await _context.Parts.FindAsync(stocktransaction.PartId);
        if (part == null)
        {
            ModelState.AddModelError("PartId", "Part not found.");
            ViewBag.Parts = new SelectList(_context.Parts.OrderBy(p => p.PartName), "PartId", "PartName", stocktransaction.PartId);
            ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
            return View(stocktransaction);
        }

        var currentStock = part.CurrentStock ?? 0;

        if (stocktransaction.TransactionType == "Stock Out" && stocktransaction.Quantity > currentStock)
        {
            ModelState.AddModelError("Quantity", $"Cannot remove {stocktransaction.Quantity} units. Only {currentStock} units in stock.");
            ViewBag.Parts = new SelectList(_context.Parts.OrderBy(p => p.PartName), "PartId", "PartName", stocktransaction.PartId);
            ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
            return View(stocktransaction);
        }

        int quantityChange = 0;
        if (stocktransaction.TransactionType == "Stock In") quantityChange = stocktransaction.Quantity.Value;
        else if (stocktransaction.TransactionType == "Stock Out") quantityChange = -stocktransaction.Quantity.Value;
        else if (stocktransaction.TransactionType == "Adjustment") quantityChange = stocktransaction.Quantity.Value;

        stocktransaction.PreviousStock = currentStock;
        stocktransaction.NewStock = currentStock + quantityChange;
        stocktransaction.TransactionDate = DateTime.Now;

        part.CurrentStock = stocktransaction.NewStock.Value;

        _context.Add(stocktransaction);
        _context.Update(part);
        // synchronize low stock alerts for this part
        LowStockAlertManager.SyncPart(_context, part.PartId);
        await _context.SaveChangesAsync();

        // Notifications: Low stock and Out of stock
        try
        {
            var prev = stocktransaction.PreviousStock ?? 0;
            var newStock = stocktransaction.NewStock ?? 0;
            var reorder = part.ReorderLevel ?? 0;
            // Roles to notify: Inventory Manager, Admin, Owner
            var invRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Inventory Manager");
            var adminRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
            var ownerRole = _context.Roles.FirstOrDefault(r => r.RoleName == "Owner");

            // Low stock: crossed from above reorder to <= reorder
            if (prev > reorder && newStock <= reorder)
            {
                var msg = $"{part.PartName} stock is {newStock} (reorder {reorder})";
                if (invRole != null) await _notifications.CreateForRoleAsync(invRole.RoleId, "LowStock", "Low stock alert", msg, HttpContext.Session.GetInt32("UserID"));
                if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "LowStock", "Low stock alert", msg, HttpContext.Session.GetInt32("UserID"));
                if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "LowStock", "Low stock alert", msg, HttpContext.Session.GetInt32("UserID"));
            }
            // Out of stock: crossed to zero
            if (prev > 0 && newStock == 0)
            {
                var msg = $"{part.PartName} is out of stock.";
                if (invRole != null) await _notifications.CreateForRoleAsync(invRole.RoleId, "OutOfStock", "Out of stock", msg, HttpContext.Session.GetInt32("UserID"));
                if (adminRole != null) await _notifications.CreateForRoleAsync(adminRole.RoleId, "OutOfStock", "Out of stock", msg, HttpContext.Session.GetInt32("UserID"));
                if (ownerRole != null) await _notifications.CreateForRoleAsync(ownerRole.RoleId, "OutOfStock", "Out of stock", msg, HttpContext.Session.GetInt32("UserID"));
            }
        }
        catch
        {
            // swallow notification errors to avoid breaking stock workflow
        }

        TempData["Success"] = $"Stock transaction created successfully. {part.PartName}: {currentStock} → {stocktransaction.NewStock}";
        return RedirectToAction(nameof(Index));
    }

    // POST: STOCKTRANSACTIONS/EditConfirmed/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditConfirmed(int transactionid, [Bind("TransactionId,Quantity,Remarks")] StockTransaction posted)
    {
        if (transactionid != posted.TransactionId) return NotFound();

        // Keep only the keys that were posted by the form to avoid unrelated validation
        // errors from other properties (Part, TransactionType, etc.).
        var allowedKeys = new[] { "TransactionId", "Quantity", "Remarks" };
        foreach (var key in ModelState.Keys.ToList())
        {
            if (!allowedKeys.Contains(key)) ModelState.Remove(key);
        }

        var existing = await _context.StockTransactions.Include(s => s.Part).FirstOrDefaultAsync(s => s.TransactionId == transactionid);
        if (existing == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(existing.ReferenceNumber) && (existing.ReferenceNumber.StartsWith("PO-") || existing.ReferenceNumber.StartsWith("JOB-")))
        {
            TempData["Error"] = "System-generated transactions cannot be edited.";
            return RedirectToAction(nameof(Edit), new { transactionid = transactionid });
        }

        if (posted.Quantity == null || posted.Quantity <= 0)
            ModelState.AddModelError("Quantity", "Quantity must be greater than zero.");

        if (!ModelState.IsValid)
        {
            // preserve user's attempted values so the form shows what they entered
            existing.Quantity = posted.Quantity;
            existing.Remarks = posted.Remarks;
            ViewBag.Parts = new SelectList(_context.Parts.OrderBy(p => p.PartName), "PartId", "PartName", existing.PartId);
            ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
            ViewBag.IsSystemGenerated = false;
            return View("Edit", existing);
        }

        var part = existing.Part;
        if (part == null)
        {
            TempData["Error"] = "Associated part not found.";
            return RedirectToAction(nameof(Index));
        }

        var oldQty = existing.Quantity ?? 0;
        var newQty = posted.Quantity.Value;
        int multiplier = existing.TransactionType == "Stock Out" ? -1 : 1;
        var delta = (newQty - oldQty) * multiplier;

        var projected = (part.CurrentStock ?? 0) + delta;
        if (projected < 0)
        {
            ModelState.AddModelError("Quantity", $"Cannot apply change: resulting stock would be negative (projected {projected}).");
            // preserve user's attempted values so the form shows what they entered
            existing.Quantity = posted.Quantity;
            existing.Remarks = posted.Remarks;
            ViewBag.Parts = new SelectList(_context.Parts.OrderBy(p => p.PartName), "PartId", "PartName", existing.PartId);
            ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
            ViewBag.IsSystemGenerated = false;
            return View("Edit", existing);
        }

        existing.Quantity = newQty;
        existing.NewStock = (existing.NewStock ?? 0) + delta;
        part.CurrentStock = projected;
        existing.Remarks = posted.Remarks;

        try
        {
            _context.Update(part);
            _context.Update(existing);
            // synchronize low stock alerts for this part
            LowStockAlertManager.SyncPart(_context, part.PartId);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Transaction updated successfully.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!StockTransactionExists(existing.TransactionId)) return NotFound();
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: STOCKTRANSACTIONS/Edit/5
    public async Task<IActionResult> Edit(int? transactionid)
    {
        if (transactionid == null) return NotFound();

        var stocktransaction = await _context.StockTransactions.Include(s => s.Part).FirstOrDefaultAsync(s => s.TransactionId == transactionid);
        if (stocktransaction == null) return NotFound();

        bool isSystemGenerated = !string.IsNullOrWhiteSpace(stocktransaction.ReferenceNumber) && (stocktransaction.ReferenceNumber.StartsWith("PO-") || stocktransaction.ReferenceNumber.StartsWith("JOB-"));
        ViewBag.IsSystemGenerated = isSystemGenerated;
        ViewBag.Parts = new SelectList(_context.Parts.OrderBy(p => p.PartName), "PartId", "PartName", stocktransaction.PartId);
        ViewBag.TransactionTypes = new[] { "Stock In", "Stock Out", "Adjustment" };
        return View(stocktransaction);
    }

    // GET: STOCKTRANSACTIONS/Delete/5
    public async Task<IActionResult> Delete(int? transactionid)
    {
        if (transactionid == null) return NotFound();

        var stocktransaction = await _context.StockTransactions.Include(s => s.Part).FirstOrDefaultAsync(m => m.TransactionId == transactionid);
        if (stocktransaction == null) return NotFound();

        bool isSystemGenerated = !string.IsNullOrWhiteSpace(stocktransaction.ReferenceNumber) && (stocktransaction.ReferenceNumber.StartsWith("PO-") || stocktransaction.ReferenceNumber.StartsWith("JOB-"));
        ViewBag.IsSystemGenerated = isSystemGenerated;
        return View(stocktransaction);
    }

    // POST: STOCKTRANSACTIONS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? transactionid)
    {
        var stocktransaction = await _context.StockTransactions.FindAsync(transactionid);
        if (stocktransaction != null)
        {
            if (!string.IsNullOrWhiteSpace(stocktransaction.ReferenceNumber) && (stocktransaction.ReferenceNumber.StartsWith("PO-") || stocktransaction.ReferenceNumber.StartsWith("JOB-")))
            {
                TempData["Error"] = "System generated transactions cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            var part = await _context.Parts.FindAsync(stocktransaction.PartId);
            if (part != null && stocktransaction.PreviousStock.HasValue)
            {
                part.CurrentStock = stocktransaction.PreviousStock.Value;
                _context.Update(part);
                // synchronize low stock alerts for this part
                LowStockAlertManager.SyncPart(_context, part.PartId);
            }

            _context.StockTransactions.Remove(stocktransaction);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Transaction deleted successfully and stock reversed.";
        }

        return RedirectToAction(nameof(Index));
    }

    private bool StockTransactionExists(int? transactionid)
    {
        return _context.StockTransactions.Any(e => e.TransactionId == transactionid);
    }
}
