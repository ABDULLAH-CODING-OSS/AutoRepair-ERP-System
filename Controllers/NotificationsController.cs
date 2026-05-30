
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: NOTIFICATIONS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Notifications.ToListAsync());
    }

    // GET: NOTIFICATIONS/Details/5
    public async Task<IActionResult> Details(int? notificationid)
    {
        if (notificationid == null)
        {
            return NotFound();
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(m => m.NotificationId == notificationid);
        if (notification == null)
        {
            return NotFound();
        }

        return View(notification);
    }

    // GET: NOTIFICATIONS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: NOTIFICATIONS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NotificationId,UserId,Title,Message,NotificationType,IsRead,CreatedAt,User")] Notification notification)
    {
        if (ModelState.IsValid)
        {
            _context.Add(notification);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(notification);
    }

    // GET: NOTIFICATIONS/Edit/5
    public async Task<IActionResult> Edit(int? notificationid)
    {
        if (notificationid == null)
        {
            return NotFound();
        }

        var notification = await _context.Notifications.FindAsync(notificationid);
        if (notification == null)
        {
            return NotFound();
        }
        return View(notification);
    }

    // POST: NOTIFICATIONS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? notificationid, [Bind("NotificationId,UserId,Title,Message,NotificationType,IsRead,CreatedAt,User")] Notification notification)
    {
        if (notificationid != notification.NotificationId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(notification);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NotificationExists(notification.NotificationId))
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
        return View(notification);
    }

    // GET: NOTIFICATIONS/Delete/5
    public async Task<IActionResult> Delete(int? notificationid)
    {
        if (notificationid == null)
        {
            return NotFound();
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(m => m.NotificationId == notificationid);
        if (notification == null)
        {
            return NotFound();
        }

        return View(notification);
    }

    // POST: NOTIFICATIONS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? notificationid)
    {
        var notification = await _context.Notifications.FindAsync(notificationid);
        if (notification != null)
        {
            _context.Notifications.Remove(notification);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool NotificationExists(int? notificationid)
    {
        return _context.Notifications.Any(e => e.NotificationId == notificationid);
    }
}
