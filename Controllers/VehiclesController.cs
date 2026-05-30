
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

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
    public async Task<IActionResult> Details(int? vehicleid)
    {
        if (vehicleid == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(m => m.VehicleId == vehicleid);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // GET: VEHICLES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: VEHICLES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("VehicleId,CustomerId,CreatedByUserId,LicensePlate,Vin,Make,Model,ManufacturingYear,Color,Mileage,EngineNumber,Notes,CreatedByUser,Customer,JobOrders")] Vehicle vehicle)
    {
        if (ModelState.IsValid)
        {
            _context.Add(vehicle);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(vehicle);
    }

    // GET: VEHICLES/Edit/5
    public async Task<IActionResult> Edit(int? vehicleid)
    {
        if (vehicleid == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles.FindAsync(vehicleid);
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
    public async Task<IActionResult> Edit(int? vehicleid, [Bind("VehicleId,CustomerId,CreatedByUserId,LicensePlate,Vin,Make,Model,ManufacturingYear,Color,Mileage,EngineNumber,Notes,CreatedByUser,Customer,JobOrders")] Vehicle vehicle)
    {
        if (vehicleid != vehicle.VehicleId)
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
    public async Task<IActionResult> Delete(int? vehicleid)
    {
        if (vehicleid == null)
        {
            return NotFound();
        }

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(m => m.VehicleId == vehicleid);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // POST: VEHICLES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? vehicleid)
    {
        var vehicle = await _context.Vehicles.FindAsync(vehicleid);
        if (vehicle != null)
        {
            _context.Vehicles.Remove(vehicle);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool VehicleExists(int? vehicleid)
    {
        return _context.Vehicles.Any(e => e.VehicleId == vehicleid);
    }
}
