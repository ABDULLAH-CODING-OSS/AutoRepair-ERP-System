
using AutoRepairERD.Filters;
using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

[SessionAuthorize]
public class VehiclesController : Controller
{
    private readonly ApplicationDbContext _context;

    public VehiclesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: VEHICLES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Vehicles.ToListAsync());
    }

    // GET: VEHICLES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(m => m.VehicleId == id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // GET: VEHICLES/Create
    //public IActionResult Create()
    //{
    //    return View();
    //}
    public IActionResult Create()
    {
        ViewBag.Customers = _context.Customers
            .Select(c => new SelectListItem
            {
                Value = c.CustomerId.ToString(),
                Text = c.FirstName + " " + c.LastName + " (" + c.Phone + ")"
            })
            .ToList();

        return View();
    }

    // POST: VEHICLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerId,LicensePlate,Vin,Make,Model,ManufacturingYear,Color,Mileage,EngineNumber,Notes")] Vehicle vehicle)
    {
        ModelState.Remove("Customer");
        ModelState.Remove("JobOrders");
        ModelState.Remove("CreatedByUser");

        if (ModelState.IsValid)
        {
            vehicle.CreatedByUserId =
    HttpContext.Session.GetInt32("UserID");
            _context.Add(vehicle);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vehicle);
    }

    // GET: VEHICLES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }
        return View(vehicle);
    }

    // POST: VEHICLES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("VehicleId,CustomerId,CreatedByUserId,LicensePlate,Vin,Make,Model,ManufacturingYear,Color,Mileage,EngineNumber,Notes,CreatedByUser,Customer,JobOrders")] Vehicle vehicle)
    {
        if (id != vehicle.VehicleId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(vehicle);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(vehicle.VehicleId))
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
        return View(vehicle);
    }

    // GET: VEHICLES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(m => m.VehicleId == id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // POST: VEHICLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var vehicle = await _context.Vehicles.FindAsync(id);
        if (vehicle != null)
        {
            _context.Vehicles.Remove(vehicle);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool VehicleExists(int? id)
    {
        return _context.Vehicles.Any(e => e.VehicleId == id);
    }
}
