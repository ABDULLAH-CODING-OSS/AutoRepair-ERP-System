
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class AuditLogsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: AUDITLOGS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.AuditLogs.ToListAsync());
    }

    // GET: AUDITLOGS/Details/5
    public async Task<IActionResult> Details(int? auditlogid)
    {
        if (auditlogid == null)
        {
            return NotFound();
        }

        var auditlog = await _context.AuditLogs
            .FirstOrDefaultAsync(m => m.AuditLogId == auditlogid);
        if (auditlog == null)
        {
            return NotFound();
        }

        return View(auditlog);
    }

    // GET: AUDITLOGS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: AUDITLOGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AuditLogId,UserId,TableName,RecordId,ActionType,OldValues,NewValues,ActionDate,Ipaddress,User")] AuditLog auditlog)
    {
        if (ModelState.IsValid)
        {
            _context.Add(auditlog);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(auditlog);
    }

    // GET: AUDITLOGS/Edit/5
    public async Task<IActionResult> Edit(int? auditlogid)
    {
        if (auditlogid == null)
        {
            return NotFound();
        }

        var auditlog = await _context.AuditLogs.FindAsync(auditlogid);
        if (auditlog == null)
        {
            return NotFound();
        }
        return View(auditlog);
    }

    // POST: AUDITLOGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? auditlogid, [Bind("AuditLogId,UserId,TableName,RecordId,ActionType,OldValues,NewValues,ActionDate,Ipaddress,User")] AuditLog auditlog)
    {
        if (auditlogid != auditlog.AuditLogId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(auditlog);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AuditLogExists(auditlog.AuditLogId))
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
        return View(auditlog);
    }

    // GET: AUDITLOGS/Delete/5
    public async Task<IActionResult> Delete(int? auditlogid)
    {
        if (auditlogid == null)
        {
            return NotFound();
        }

        var auditlog = await _context.AuditLogs
            .FirstOrDefaultAsync(m => m.AuditLogId == auditlogid);
        if (auditlog == null)
        {
            return NotFound();
        }

        return View(auditlog);
    }

    // POST: AUDITLOGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? auditlogid)
    {
        var auditlog = await _context.AuditLogs.FindAsync(auditlogid);
        if (auditlog != null)
        {
            _context.AuditLogs.Remove(auditlog);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AuditLogExists(int? auditlogid)
    {
        return _context.AuditLogs.Any(e => e.AuditLogId == auditlogid);
    }
}
