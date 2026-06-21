
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Filters;

[RoleAuthorize("Admin","Owner")]
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
        // Provide search and filter parameters via query string
        var q = HttpContext.Request.Query["q"].ToString();
        var module = HttpContext.Request.Query["module"].ToString();
        var range = HttpContext.Request.Query["range"].ToString(); // today,7,30,all
        var page = 1;
        var pageSize = 50;
        int.TryParse(HttpContext.Request.Query["page"].ToString(), out page);
        int.TryParse(HttpContext.Request.Query["pageSize"].ToString(), out pageSize);
        if (page < 1) page = 1;
        if (pageSize <= 0) pageSize = 50;

        var query = _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(a => (a.User != null && (a.User.Username ?? "").Contains(q)) || (a.ActionType ?? "").Contains(q));
        }

        // Validate module against whitelist to avoid arbitrary input
        var allowedModules = new[] { "Payrolls", "SalaryAdjustments", "Notifications", "JobOrders", "PurchaseOrders", "Invoices", "Payments" };
        if (!string.IsNullOrWhiteSpace(module) && module != "All")
        {
            if (allowedModules.Contains(module))
            {
                query = query.Where(a => (a.TableName ?? "") == module);
            }
            else
            {
                // ignore invalid module filter
                module = "All";
            }
        }

        if (range == "today")
        {
            var today = DateTime.Today;
            query = query.Where(a => a.ActionDate >= today);
        }
        else if (range == "7")
        {
            var from = DateTime.Now.AddDays(-7);
            query = query.Where(a => a.ActionDate >= from);
        }
        else if (range == "30")
        {
            var from = DateTime.Now.AddDays(-30);
            query = query.Where(a => a.ActionDate >= from);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        if (page > totalPages) page = totalPages > 0 ? totalPages : 1;
        var list = await query.OrderByDescending(a => a.ActionDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = totalPages;
        ViewBag.Query = q;
        ViewBag.Module = module;
        ViewBag.Range = range;

        return View(list);
    }

    // GET: AUDITLOGS/Details/5
    public async Task<IActionResult> Details(int? auditlogid)
    {
        if (auditlogid == null)
        {
            return NotFound();
        }

        var auditlog = await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
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
        // Creation of audit logs via UI is disabled. Audits are system-generated only.
        return RedirectToAction(nameof(Index));
    }

    // POST: AUDITLOGS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AuditLogId,UserId,TableName,RecordId,ActionType,OldValues,NewValues,ActionDate,Ipaddress,User")] AuditLog auditlog)
    {
        // System-generated only. Do not allow manual creation.
        return RedirectToAction(nameof(Index));
    }

    // GET: AUDITLOGS/Edit/5
    public async Task<IActionResult> Edit(int? auditlogid)
    {
        // Editing audit logs is not allowed.
        return RedirectToAction(nameof(Index));
    }

    // POST: AUDITLOGS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? auditlogid, [Bind("AuditLogId,UserId,TableName,RecordId,ActionType,OldValues,NewValues,ActionDate,Ipaddress,User")] AuditLog auditlog)
    {
        // Editing audit logs is not allowed.
        return RedirectToAction(nameof(Index));
    }

    // GET: AUDITLOGS/Delete/5
    public async Task<IActionResult> Delete(int? auditlogid)
    {
        // Deletion of audit logs is not allowed.
        return RedirectToAction(nameof(Index));
    }

    // POST: AUDITLOGS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? auditlogid)
    {
        // Deletion of audit logs is not allowed.
        return RedirectToAction(nameof(Index));
    }

    private bool AuditLogExists(int? auditlogid)
    {
        return _context.AuditLogs.Any(e => e.AuditLogId == auditlogid);
    }
}
