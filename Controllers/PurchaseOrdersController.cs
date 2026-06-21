
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using AutoRepairERD.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
[RoleAuthorize("Admin","Owner","Inventory Manager")]

public class PurchaseOrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AutoRepairERD.Services.NotificationService _notifications;

    public PurchaseOrdersController(ApplicationDbContext context, AutoRepairERD.Services.NotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    // GET: PURCHASEORDERS
    public async Task<IActionResult> Index()    
    {
        var list = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .ToListAsync();

        return View(list);
    }

    // GET: PURCHASEORDERS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var purchaseorder = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
            .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(m => m.PurchaseOrderId == id);
        if (purchaseorder == null)
        {
            return NotFound();
        }

        return View(purchaseorder);
    }

    // GET: PURCHASEORDERS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.Suppliers = _context.Suppliers
            .Select(s => new SelectListItem
            {
                Value = s.SupplierId.ToString(),
                Text = s.CompanyName
            })
            .ToList();

        return View();
    }

    // POST: PURCHASEORDERS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PurchaseOrderId,SupplierId,CreatedByUserId,OrderDate,ExpectedDeliveryDate,Status,TotalAmount,CreatedByUser,PurchaseOrderItems,Supplier")] PurchaseOrder purchaseorder)
    {
        ModelState.Remove("CreatedByUser");
        ModelState.Remove("PurchaseOrderItems");
        ModelState.Remove("Supplier");
        purchaseorder.CreatedByUserId =
    HttpContext.Session.GetInt32("UserID");

        purchaseorder.OrderDate =
            DateOnly.FromDateTime(DateTime.Now);

        purchaseorder.Status = "Ordered";
        // Validate ExpectedDeliveryDate
        if (!purchaseorder.ExpectedDeliveryDate.HasValue)
        {
            ModelState.AddModelError("ExpectedDeliveryDate", "Expected Delivery Date is required.");
        }
        else if (purchaseorder.ExpectedDeliveryDate < purchaseorder.OrderDate)
        {
            ModelState.AddModelError("ExpectedDeliveryDate", "Expected Delivery Date cannot be before the Order Date.");
        }
                if (ModelState.IsValid)
        {
            _context.Add(purchaseorder);
            await _context.SaveChangesAsync();
            // Notify inventory managers that a new PO is created
            try
            {
                var inv = _context.Roles.FirstOrDefault(r => r.RoleName == "Inventory Manager");
                var admin = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                var owner = _context.Roles.FirstOrDefault(r => r.RoleName == "Owner");
                var msg = $"Purchase Order #{purchaseorder.PurchaseOrderId} created.";
                if (inv != null) await _notifications.CreateForRoleAsync(inv.RoleId, "PurchaseOrderCreated", "New Purchase Order", msg, HttpContext.Session.GetInt32("UserID"));
                if (admin != null) await _notifications.CreateForRoleAsync(admin.RoleId, "PurchaseOrderCreated", "New Purchase Order", msg, HttpContext.Session.GetInt32("UserID"));
                if (owner != null) await _notifications.CreateForRoleAsync(owner.RoleId, "PurchaseOrderCreated", "New Purchase Order", msg, HttpContext.Session.GetInt32("UserID"));
            }
            catch { }

            // Audit: Purchase Order Created (best-effort)
            try
            {
                var supplier = await _context.Suppliers.FindAsync(purchaseorder.SupplierId);
                var supplierName = supplier != null ? supplier.CompanyName : "";
                var audit = new AuditLog
                {
                    UserId = HttpContext.Session.GetInt32("UserID"),
                    TableName = "PurchaseOrders",
                    RecordId = purchaseorder.PurchaseOrderId,
                    ActionType = "Purchase Order Created",
                    OldValues = null,
                    NewValues = $"PONumber={purchaseorder.PurchaseOrderId};Supplier={supplierName};Status={purchaseorder.Status};TotalAmount={purchaseorder.TotalAmount}",
                    ActionDate = DateTime.Now,
                    Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch { }

            return RedirectToAction(nameof(Index));
        }
        ViewBag.Suppliers = _context.Suppliers
    .Select(s => new SelectListItem
    {
        Value = s.SupplierId.ToString(),
        Text = s.CompanyName
    })
    .ToList();
        return View(purchaseorder);
    }

    // GET: PURCHASEORDERS/Edit/5
    //public async Task<IActionResult> Edit(int? id)
    //{
    //    if (id == null)
    //    {
    //        return NotFound();
    //    }

    //    var purchaseorder = await _context.PurchaseOrders.FindAsync(id  );
    //    if (purchaseorder == null)
    //    {
    //        return NotFound();
    //    }
    //    return View(purchaseorder);
    //}
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var purchaseorder = await _context.PurchaseOrders.FindAsync(id);

        if (purchaseorder == null)
        {
            return NotFound();
        }

        ViewBag.Suppliers = new SelectList(
            _context.Suppliers,
            "SupplierId",
            "CompanyName",
            purchaseorder.SupplierId
        );

        // If already received, show locked message on the edit page rather than redirecting
        if (purchaseorder.Status == "Received")
        {
            ViewBag.Locked = true;
            ModelState.AddModelError(string.Empty, "This purchase order has already been received and can no longer be modified.");
        }

        return View(purchaseorder);
    }

    // POST: PURCHASEORDERS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("PurchaseOrderId,SupplierId,CreatedByUserId,OrderDate,ExpectedDeliveryDate,Status,TotalAmount,CreatedByUser,PurchaseOrderItems,Supplier")] PurchaseOrder purchaseorder)
    {
        ModelState.Remove("CreatedByUser");
        ModelState.Remove("PurchaseOrderItems");
        ModelState.Remove("Supplier");
        if (id != purchaseorder.PurchaseOrderId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            //try
            //{
            //    _context.Update(purchaseorder);
            //    await _context.SaveChangesAsync();
            //}
            try
            {
                var existingPO = await _context.PurchaseOrders
                    .Include(p => p.PurchaseOrderItems)
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseorder.PurchaseOrderId);

                if (existingPO == null)
                {
                    return NotFound();
                }

                // Prevent modifying a purchase order that has already been received
                if (existingPO.Status == "Received")
                {
                    ModelState.AddModelError(string.Empty, "This purchase order has already been received and can no longer be modified.");
                    ViewBag.Suppliers = new SelectList(
                        _context.Suppliers,
                        "SupplierId",
                        "CompanyName",
                        purchaseorder.SupplierId
                    );
                    ViewBag.Locked = true;
                    return View(purchaseorder);
                }

                bool stockAlreadyReceived = existingPO.Status == "Received";

                existingPO.SupplierId = purchaseorder.SupplierId;
                // Validate ExpectedDeliveryDate
                if (!purchaseorder.ExpectedDeliveryDate.HasValue)
                {
                    ModelState.AddModelError("ExpectedDeliveryDate", "Expected Delivery Date is required.");
                }
                else if (purchaseorder.ExpectedDeliveryDate < existingPO.OrderDate)
                {
                    ModelState.AddModelError("ExpectedDeliveryDate", "Expected Delivery Date cannot be before the Order Date.");
                }

                existingPO.ExpectedDeliveryDate = purchaseorder.ExpectedDeliveryDate;
                existingPO.Status = purchaseorder.Status;
                // TotalAmount is system-calculated; do not accept manual edits
                // existingPO.TotalAmount = purchaseorder.TotalAmount;

                if (!stockAlreadyReceived && purchaseorder.Status == "Received")
                {
                    var poItems = await _context.PurchaseOrderItems
                        .Where(x => x.PurchaseOrderId == purchaseorder.PurchaseOrderId)
                        .ToListAsync();

                    foreach (var item in poItems)
                    {
                        var qty = item.Quantity ?? 0;
                        if (qty <= 0) continue; // nothing to receive for this line

                        var part = await _context.Parts.FindAsync(item.PartId);
                        if (part == null) continue;

                        // Build a per-line reference to avoid duplicate per PO item
                        var refNumber = $"PO-{purchaseorder.PurchaseOrderId}-{item.PoitemId}";

                        // Extra duplicate protection: check part, ref and amount
                        var existsTransaction = await _context.StockTransactions
                            .AnyAsync(s => s.ReferenceNumber == refNumber
                                           && s.PartId == item.PartId
                                           && s.Quantity == qty
                                           && s.TransactionType == "Stock In");

                        if (existsTransaction) continue;

                        var previousStock = part.CurrentStock ?? 0;
                        var newStock = previousStock + qty;

                        var supplierName = _context.Suppliers.Where(s => s.SupplierId == purchaseorder.SupplierId)
                            .Select(s => s.CompanyName).FirstOrDefault();

                        var stockTransaction = new StockTransaction
                        {
                            PartId = item.PartId,
                            TransactionType = "Stock In",
                            Quantity = qty,
                            PreviousStock = previousStock,
                            NewStock = newStock,
                            ReferenceNumber = refNumber,
                            TransactionDate = DateTime.Now,
                            Remarks = $"Purchase Order #{purchaseorder.PurchaseOrderId} received from {supplierName}"
                        };

                        _context.StockTransactions.Add(stockTransaction);
                        part.CurrentStock = newStock;
                        _context.Update(part);
                        // sync low stock alerts after PO receive (pass user/ip for audit enrichment)
                        LowStockAlertManager.SyncPart(_context, part.PartId, HttpContext.Session.GetInt32("UserID"), HttpContext.Connection.RemoteIpAddress?.ToString());
                        // synchronize low stock alerts for this part (duplicate call retained for compatibility)
                        LowStockAlertManager.SyncPart(_context, part.PartId, HttpContext.Session.GetInt32("UserID"), HttpContext.Connection.RemoteIpAddress?.ToString());
                    }
                }

                await _context.SaveChangesAsync();

                // Notify after PO received
                try
                {
                    var inv = _context.Roles.FirstOrDefault(r => r.RoleName == "Inventory Manager");
                    var admin = _context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                    var owner = _context.Roles.FirstOrDefault(r => r.RoleName == "Owner");
                    var msg = $"Purchase Order #{purchaseorder.PurchaseOrderId} has been received and stock updated.";
                    if (inv != null) await _notifications.CreateForRoleAsync(inv.RoleId, "PurchaseOrderReceived", "Purchase Order received", msg, HttpContext.Session.GetInt32("UserID"));
                    if (admin != null) await _notifications.CreateForRoleAsync(admin.RoleId, "PurchaseOrderReceived", "Purchase Order received", msg, HttpContext.Session.GetInt32("UserID"));
                    if (owner != null) await _notifications.CreateForRoleAsync(owner.RoleId, "PurchaseOrderReceived", "Purchase Order received", msg, HttpContext.Session.GetInt32("UserID"));
                }
                catch { }
                // Audit: Purchase Order Received / Updated (best-effort)
                try
                {
                    var supplier = await _context.Suppliers.FindAsync(purchaseorder.SupplierId);
                    var supplierName = supplier != null ? supplier.CompanyName : "";
                    var oldStatus = existingPO.Status;
                    var newStatus = purchaseorder.Status;
                    var actionType = "Purchase Order Updated";
                    if (oldStatus != newStatus && newStatus == "Received") actionType = "Purchase Order Received";

                    var audit = new AuditLog
                    {
                        UserId = HttpContext.Session.GetInt32("UserID"),
                        TableName = "PurchaseOrders",
                        RecordId = purchaseorder.PurchaseOrderId,
                        ActionType = actionType,
                        OldValues = $"Status={oldStatus}",
                        NewValues = $"Status={newStatus};Supplier={supplierName};TotalAmount={purchaseorder.TotalAmount}",
                        ActionDate = DateTime.Now,
                        Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };
                    _context.AuditLogs.Add(audit);
                    await _context.SaveChangesAsync();
                }
                catch { }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderExists(purchaseorder.PurchaseOrderId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        //    ViewBag.Suppliers = _context.Suppliers
        //.Select(s => new SelectListItem
        //{
        //    Value = s.SupplierId.ToString(),
        //    Text = s.CompanyName
        //})
        //.ToList();
        ViewBag.Suppliers = new SelectList(
        _context.Suppliers,
        "SupplierId",
        "CompanyName",
        purchaseorder.SupplierId
    );
        return View(purchaseorder);
        //return View(purchaseorder);
    }

    // GET: PURCHASEORDERS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var purchaseorder = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
            .FirstOrDefaultAsync(m => m.PurchaseOrderId == id);
        if (purchaseorder == null)
        {
            return NotFound();
        }

        ViewBag.HasItems = purchaseorder.PurchaseOrderItems != null && purchaseorder.PurchaseOrderItems.Any();

        return View(purchaseorder);
    }

    // POST: PURCHASEORDERS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var purchaseorder = await _context.PurchaseOrders.FindAsync(id);
        if (purchaseorder != null && purchaseorder.Status == "Received")
        {
            TempData["Error"] = "This purchase order has already been received and cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }
        if (purchaseorder != null)
        {
            _context.PurchaseOrders.Remove(purchaseorder);
        }
        try
        {
            await _context.SaveChangesAsync();
            // Audit: Purchase Order Deleted (best-effort)
            try
            {
                var supplier = await _context.Suppliers.FindAsync(purchaseorder.SupplierId);
                var supplierName = supplier != null ? supplier.CompanyName : "";
                var audit = new AuditLog
                {
                    UserId = HttpContext.Session.GetInt32("UserID"),
                    TableName = "PurchaseOrders",
                    RecordId = purchaseorder.PurchaseOrderId,
                    ActionType = "Purchase Order Deleted",
                    OldValues = $"PONumber={purchaseorder.PurchaseOrderId};Supplier={supplierName};Status={purchaseorder.Status};TotalAmount={purchaseorder.TotalAmount}",
                    NewValues = null,
                    ActionDate = DateTime.Now,
                    Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _context.AuditLogs.Add(audit);
                await _context.SaveChangesAsync();
            }
            catch { }
        }
        catch (DbUpdateException ex)
        {
            // Likely a foreign key constraint (items exist). Provide a friendly message instead of crashing.
            TempData["Error"] = "Cannot delete this purchase order because it contains items. Please remove all items from the order before deleting it.";
            return RedirectToAction(nameof(Delete), new { id });
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PurchaseOrderExists(int? id)
    {
        return _context.PurchaseOrders.Any(e => e.PurchaseOrderId == id);
    }
}
