
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
[SessionAuthorize]
public class JobPartItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public JobPartItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: JOBPARTITEMS
    public async Task<IActionResult> Index()    
    {
        var items = await _context.JobPartItems
            .Include(j => j.JobOrder)
            .Include(j => j.Part)
            .ToListAsync();

        return View(items);
    }

    // GET: JOBPARTITEMS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobpartitem = await _context.JobPartItems
            .Include(j => j.JobOrder)
            .Include(j => j.Part)
            .FirstOrDefaultAsync(m => m.JobPartItemId == id);
        if (jobpartitem == null)
        {
            return NotFound();
        }

        return View(jobpartitem);
    }

    // GET: JOBPARTITEMS/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.JobOrders = _context.JobOrders
            .Where(j => j.Status != "Completed" && j.Status != "Cancelled")
            .Select(j => new SelectListItem
            {
                Value = j.JobOrderId.ToString(),
                Text = j.JobNumber
            })
            .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = p.PartName + " (Stock: " + p.CurrentStock + ")"
            })
            .ToList();


        return View();
    }

    // POST: JOBPARTITEMS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("JobPartItemId,JobOrderId,PartId,Quantity,UnitPrice,TotalPrice,JobOrder,Part")] JobPartItem jobpartitem)
    {

        ModelState.Remove("JobOrder");
        ModelState.Remove("Part");
        if (!ModelState.IsValid)
        {
            foreach (var item in ModelState)
            {
                foreach (var error in item.Value.Errors)
                {
                    Console.WriteLine($"{item.Key}: {error.ErrorMessage}");
                }
            }
        }
        //if (ModelState.IsValid)
        //{
        //    _context.Add(jobpartitem);
        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}
        if (ModelState.IsValid)
        {
            var part = await _context.Parts.FindAsync(jobpartitem.PartId);

            if (part == null)
            {
                ModelState.AddModelError("", "Part not found.");
            }
            else if (part.CurrentStock < jobpartitem.Quantity)
            {
                ModelState.AddModelError("", "Insufficient Stock Available.");
            }
            else
            {
                jobpartitem.UnitPrice = part.SalePrice;
                jobpartitem.TotalPrice =
                    part.SalePrice * jobpartitem.Quantity;

                part.CurrentStock -= jobpartitem.Quantity;

                _context.JobPartItems.Add(jobpartitem);
                var transaction = new StockTransaction
                {
                    PartId = part.PartId,
                    TransactionType = "OUT",
                    Quantity = jobpartitem.Quantity,
                    PreviousStock = part.CurrentStock + jobpartitem.Quantity,
                    NewStock = part.CurrentStock,
                    ReferenceNumber = "JOB-" + jobpartitem.JobOrderId,
                    Remarks = "Part used in Job Order",
                    TransactionDate = DateTime.Now
                };

                _context.StockTransactions.Add(transaction);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
        }
        ViewBag.JobOrders = _context.JobOrders
    .Where(j => j.Status != "Completed" && j.Status != "Cancelled")
    .Select(j => new SelectListItem
    {
        Value = j.JobOrderId.ToString(),
        Text = j.JobNumber
    })
    .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem
            {
                Value = p.PartId.ToString(),
                Text = p.PartName + " (Stock: " + p.CurrentStock + ")"
            })
            .ToList();
        return View(jobpartitem);
    }

    // GET: JOBPARTITEMS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobpartitem = await _context.JobPartItems
            .Include(j => j.Part)
            .Include(j => j.JobOrder)
            .FirstOrDefaultAsync(j => j.JobPartItemId == id);
        if (jobpartitem == null)
        {
            return NotFound();
        }
        // Provide select lists in case view needs them (kept minimal)
        ViewBag.JobOrders = _context.JobOrders
            .Where(j => j.Status != "Completed" && j.Status != "Cancelled")
            .Select(j => new SelectListItem { Value = j.JobOrderId.ToString(), Text = j.JobNumber })
            .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem { Value = p.PartId.ToString(), Text = p.PartName + " (Stock: " + p.CurrentStock + ")" })
            .ToList();

        return View(jobpartitem);
    }

    // POST: JOBPARTITEMS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("JobPartItemId,JobOrderId,PartId,Quantity,UnitPrice,TotalPrice,JobOrder,Part")] JobPartItem jobpartitem)
    {
        if (id != jobpartitem.JobPartItemId)
        {
            return NotFound();
        }
        // Remove navigation props from validation
        ModelState.Remove("JobOrder");
        ModelState.Remove("Part");

        if (ModelState.IsValid)
        {
            try
            {
                // Ensure unit price and total price reflect current part sale price
                var part = await _context.Parts.FindAsync(jobpartitem.PartId);
                if (part != null)
                {
                    jobpartitem.UnitPrice = part.SalePrice;
                    jobpartitem.TotalPrice = part.SalePrice * jobpartitem.Quantity;
                }

                _context.Update(jobpartitem);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobPartItemExists(jobpartitem.JobPartItemId))
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

        // repopulate selects for the view
        ViewBag.JobOrders = _context.JobOrders
            .Where(j => j.Status != "Completed" && j.Status != "Cancelled")
            .Select(j => new SelectListItem { Value = j.JobOrderId.ToString(), Text = j.JobNumber })
            .ToList();

        ViewBag.Parts = _context.Parts
            .Select(p => new SelectListItem { Value = p.PartId.ToString(), Text = p.PartName + " (Stock: " + p.CurrentStock + ")" })
            .ToList();

        return View(jobpartitem);
    }

    // GET: JOBPARTITEMS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobpartitem = await _context.JobPartItems
            .FirstOrDefaultAsync(m => m.JobPartItemId == id);
        if (jobpartitem == null)
        {
            return NotFound();
        }

        return View(jobpartitem);
    }

    // POST: JOBPARTITEMS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var jobpartitem = await _context.JobPartItems.FindAsync(id);
        if (jobpartitem != null)
        {
            _context.JobPartItems.Remove(jobpartitem);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool JobPartItemExists(int? id)
    {
        return _context.JobPartItems.Any(e => e.JobPartItemId == id);
    }
}
