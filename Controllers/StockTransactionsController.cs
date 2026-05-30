
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class StockTransactionsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StockTransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: STOCKTRANSACTIONS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.StockTransactions.ToListAsync());
    }

    // GET: STOCKTRANSACTIONS/Details/5
    public async Task<IActionResult> Details(int? transactionid)
    {
        if (transactionid == null)
        {
            return NotFound();
        }

        var stocktransaction = await _context.StockTransactions
            .FirstOrDefaultAsync(m => m.TransactionId == transactionid);
        if (stocktransaction == null)
        {
            return NotFound();
        }

        return View(stocktransaction);
    }

    // GET: STOCKTRANSACTIONS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: STOCKTRANSACTIONS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TransactionId,PartId,TransactionType,Quantity,PreviousStock,NewStock,ReferenceNumber,Remarks,TransactionDate,Part")] StockTransaction stocktransaction)
    {
        if (ModelState.IsValid)
        {
            _context.Add(stocktransaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(stocktransaction);
    }

    // GET: STOCKTRANSACTIONS/Edit/5
    public async Task<IActionResult> Edit(int? transactionid)
    {
        if (transactionid == null)
        {
            return NotFound();
        }

        var stocktransaction = await _context.StockTransactions.FindAsync(transactionid);
        if (stocktransaction == null)
        {
            return NotFound();
        }
        return View(stocktransaction);
    }

    // POST: STOCKTRANSACTIONS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? transactionid, [Bind("TransactionId,PartId,TransactionType,Quantity,PreviousStock,NewStock,ReferenceNumber,Remarks,TransactionDate,Part")] StockTransaction stocktransaction)
    {
        if (transactionid != stocktransaction.TransactionId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(stocktransaction);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StockTransactionExists(stocktransaction.TransactionId))
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
        return View(stocktransaction);
    }

    // GET: STOCKTRANSACTIONS/Delete/5
    public async Task<IActionResult> Delete(int? transactionid)
    {
        if (transactionid == null)
        {
            return NotFound();
        }

        var stocktransaction = await _context.StockTransactions
            .FirstOrDefaultAsync(m => m.TransactionId == transactionid);
        if (stocktransaction == null)
        {
            return NotFound();
        }

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
            _context.StockTransactions.Remove(stocktransaction);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool StockTransactionExists(int? transactionid)
    {
        return _context.StockTransactions.Any(e => e.TransactionId == transactionid);
    }
}
