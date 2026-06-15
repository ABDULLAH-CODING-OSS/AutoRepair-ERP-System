
using Microsoft.AspNetCore.Mvc;
using AutoRepairERD.Filters;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;
using AutoRepairERD.Services;

[SessionAuthorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _service;

    public NotificationsController(ApplicationDbContext context, NotificationService service)
    {
        _context = context;
        _service = service;
    }

    // GET: NOTIFICATIONS
        public async Task<IActionResult> Index()
    {
        var uid = HttpContext.Session.GetInt32("UserID");
        if (uid == null) return Forbid();
            var list = await _context.Notifications
                .Include(n => n.User)
                .Where(n => n.UserId == uid.Value)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(list);
    }

    // GET: NOTIFICATIONS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var n = await _context.Notifications.Include(x => x.User).FirstOrDefaultAsync(x => x.NotificationId == id);
        if (n == null) return NotFound();
        var uid = HttpContext.Session.GetInt32("UserID");
        if (uid == null) return Forbid();
        if (!await UserHasAccessAsync(uid.Value, n)) return Forbid();

        await _service.MarkReadAsync(n.NotificationId);
        return View(n);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var uid = HttpContext.Session.GetInt32("UserID");
        if (uid == null) return Forbid();
        var n = await _context.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        if (!await UserHasAccessAsync(uid.Value, n)) return Forbid();

        await _service.MarkReadAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkUnread(int id)
    {
        var uid = HttpContext.Session.GetInt32("UserID");
        if (uid == null) return Forbid();
        var n = await _context.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        if (!await UserHasAccessAsync(uid.Value, n)) return Forbid();

        await _service.MarkUnreadAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var uid = HttpContext.Session.GetInt32("UserID");
        if (uid == null) return Forbid();
        var n = await _context.Notifications.FindAsync(id);
        if (n == null) return NotFound();
        if (!await UserHasAccessAsync(uid.Value, n)) return Forbid();

        await _service.ArchiveAsync(id);
        return RedirectToAction(nameof(Index));
    }

        private async Task<bool> UserHasAccessAsync(int userId, Notification n)
        {
            // Notifications are created per-user in current DB schema. Access allowed when UserId matches.
            return n.UserId != null && n.UserId == userId;
        }
}
