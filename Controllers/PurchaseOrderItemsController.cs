
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[RoleAuthorize("Admin","Owner","Inventory Manager")]

public class PurchaseOrderItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: PURCHASEORDERITEMS
    public async Task<IActionResult> Index()    
    {
        var list = await _context.PurchaseOrderItems
            .Include(i => i.Part)
            .Include(i => i.PurchaseOrder)
            .ToListAsync();

        return View(list);
    }

    // GET: PURCHASEORDERITEMS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var purchaseorderitem = await _context.PurchaseOrderItems
            .Include(i => i.Part)
            .Include(i => i.PurchaseOrder)
            .FirstOrDefaultAsync(m => m.PoitemId == id);
        if (purchaseorderitem == null)
        {
            return NotFound();
        }

        return View(purchaseorderitem);
    }

    // GET: PURCHASEORDERITEMS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create(int? purchaseOrderId)
    {
        var poList = _context.PurchaseOrders
            .Where(po => po.Status != "Received")
            .Select(po => new SelectListItem
            {
                Value = po.PurchaseOrderId.ToString(),
                Text = "PO-" + po.PurchaseOrderId
            })
            .ToList();
        if (purchaseOrderId.HasValue)
        {
            var match = poList.FirstOrDefault(x => x.Value == purchaseOrderId.Value.ToString());
            if (match != null) match.Selected = true;
        }
        ViewBag.PurchaseOrders = poList;

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = p.PartName
            })
            .ToList();

        return View();
    }
    // POST: PURCHASEORDERITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("PurchaseOrderId,PartId,Quantity")] PurchaseOrderItem purchaseorderitem)
    {
        ModelState.Remove("Part");
        ModelState.Remove("PurchaseOrder");
        // UnitCost and TotalCost are server-calculated; remove them so validation doesn't fail when they are not posted.
        ModelState.Remove("UnitCost");
        ModelState.Remove("TotalCost");
        //if (ModelState.IsValid)
        //{
        //    _context.Add(purchaseorderitem);
        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}
        if (ModelState.IsValid)
        {
            // Prevent adding items to received purchase orders
            var parentPo = await _context.PurchaseOrders
                .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseorderitem.PurchaseOrderId);

            if (parentPo == null)
            {
                ModelState.AddModelError("PurchaseOrderId", "Selected purchase order was not found.");
            }
            else if (parentPo.Status == "Received")
            {
                ModelState.AddModelError(string.Empty, "This purchase order has already been received and can no longer be modified.");
            }
            else
            {
                var part = await _context.Parts.FindAsync(purchaseorderitem.PartId);

                if (part != null)
                {
                    purchaseorderitem.UnitCost = part.CostPrice;
                    purchaseorderitem.TotalCost = (part.CostPrice * (purchaseorderitem.Quantity ?? 0));
                }

                _context.PurchaseOrderItems.Add(purchaseorderitem);
                await _context.SaveChangesAsync();

                // Recalculate parent purchase order total
                await UpdatePurchaseOrderTotalAsync(purchaseorderitem.PurchaseOrderId);

                return RedirectToAction(nameof(Index));
            }
        }
        ViewBag.PurchaseOrders = _context.PurchaseOrders
            .Where(po => po.Status != "Received")
            .Select(po => new SelectListItem
            {
                Value = po.PurchaseOrderId.ToString(),
                Text = "PO-" + po.PurchaseOrderId
            })
            .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = p.PartName
            })
            .ToList();
        return View(purchaseorderitem);
    }

    // GET: PURCHASEORDERITEMS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var purchaseorderitem = await _context.PurchaseOrderItems.FindAsync(id);
        if (purchaseorderitem == null)
        {
            return NotFound();
        }
        ViewBag.PurchaseOrders = _context.PurchaseOrders
            .Select(po => new SelectListItem
            {
                Value = po.PurchaseOrderId.ToString(),
                Text = "PO-" + po.PurchaseOrderId
            })
            .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = p.PartName
            })
            .ToList();

        // Prevent editing if parent PO is already received - show message on the edit page
        var parentPo = await _context.PurchaseOrders.FindAsync(purchaseorderitem.PurchaseOrderId);
        if (parentPo != null && parentPo.Status == "Received")
        {
            ViewBag.Locked = true;
            ModelState.AddModelError(string.Empty, "This purchase order has already been received and can no longer be modified.");
        }

        return View(purchaseorderitem);
    }

    // POST: PURCHASEORDERITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("PoitemId,PurchaseOrderId,PartId,Quantity")] PurchaseOrderItem purchaseorderitem)
    {
        if (id != purchaseorderitem.PoitemId)
        {
            return NotFound();
        }

        // Remove server/computed properties and navigation properties from modelstate
        // so they don't cause validation to fail when they are not posted by the form.
        ModelState.Remove("Part");
        ModelState.Remove("PurchaseOrder");
        ModelState.Remove("UnitCost");
        ModelState.Remove("TotalCost");

        if (ModelState.IsValid)
        {
            try
            {
                // Prevent editing items if parent PO is received
                var parentPo = await _context.PurchaseOrders
                    .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseorderitem.PurchaseOrderId);

                if (parentPo != null && parentPo.Status == "Received")
                {
                    ModelState.AddModelError(string.Empty, "This purchase order has already been received and can no longer be modified.");
                    // repopulate dropdowns for redisplay
                    ViewBag.PurchaseOrders = _context.PurchaseOrders
                        .Select(po => new SelectListItem
                        {
                            Value = po.PurchaseOrderId.ToString(),
                            Text = "PO-" + po.PurchaseOrderId
                        })
                        .ToList();

                    ViewBag.Parts = _context.Parts
                        .Select(p => new SelectListItem
                        {
                            Value = p.PartId.ToString(),
                            Text = p.PartName
                        })
                        .ToList();

                    return View(purchaseorderitem);
                }

                // Load existing entity to preserve other fields
                var existing = await _context.PurchaseOrderItems.FindAsync(purchaseorderitem.PoitemId);
                if (existing == null)
                    return NotFound();

                var oldPurchaseOrderId = existing.PurchaseOrderId;

                existing.PurchaseOrderId = purchaseorderitem.PurchaseOrderId;
                existing.PartId = purchaseorderitem.PartId;
                existing.Quantity = purchaseorderitem.Quantity;

                var part = await _context.Parts.FindAsync(existing.PartId);
                if (part != null)
                {
                    existing.UnitCost = part.CostPrice;
                }
                existing.TotalCost = (existing.UnitCost ?? 0) * (existing.Quantity ?? 0);

                _context.Update(existing);
                await _context.SaveChangesAsync();

                // Recalculate parent PO total(s)
                // If the item was moved to a different purchase order, update both old and new totals
                if (oldPurchaseOrderId != existing.PurchaseOrderId)
                {
                    await UpdatePurchaseOrderTotalAsync(oldPurchaseOrderId);
                }
                await UpdatePurchaseOrderTotalAsync(existing.PurchaseOrderId);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PurchaseOrderItemExists(purchaseorderitem.PoitemId))
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
        // repopulate dropdowns
        ViewBag.PurchaseOrders = _context.PurchaseOrders
            .Select(po => new SelectListItem
            {
                Value = po.PurchaseOrderId.ToString(),
                Text = "PO-" + po.PurchaseOrderId
            })
            .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = p.PartName
            })
            .ToList();

        return View(purchaseorderitem);
    }

    // GET: PURCHASEORDERITEMS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var purchaseorderitem = await _context.PurchaseOrderItems
            .Include(i => i.Part)
            .Include(i => i.PurchaseOrder)
            .FirstOrDefaultAsync(m => m.PoitemId == id);
        if (purchaseorderitem == null)
        {
            return NotFound();
        }

        return View(purchaseorderitem);
    }

    // POST: PURCHASEORDERITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var purchaseorderitem = await _context.PurchaseOrderItems.FindAsync(id);
        if (purchaseorderitem != null)
        {
            var parentPo = await _context.PurchaseOrders.FindAsync(purchaseorderitem.PurchaseOrderId);
            if (parentPo != null && parentPo.Status == "Received")
            {
                TempData["Error"] = "This purchase order has already been received and can no longer be modified.";
                return RedirectToAction(nameof(Index));
            }

            _context.PurchaseOrderItems.Remove(purchaseorderitem);
        }

        await _context.SaveChangesAsync();

        if (purchaseorderitem != null)
        {
            await UpdatePurchaseOrderTotalAsync(purchaseorderitem.PurchaseOrderId);
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PurchaseOrderItemExists(int? id)
    {
        return _context.PurchaseOrderItems.Any(e => e.PoitemId == id);
    }

    private async Task UpdatePurchaseOrderTotalAsync(int purchaseOrderId)
    {
        var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
        if (po == null) return;

        var total = await _context.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == purchaseOrderId)
            .SumAsync(i => (decimal?) (i.TotalCost ?? 0));

        po.TotalAmount = total ?? 0m;
        await _context.SaveChangesAsync();
    }
}
