using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoRepairERD.Models;

namespace AutoRepairERD.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PartsApiController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/parts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PartDto>>> GetAll()
        {
            var list = await _context.Parts
                .AsNoTracking()
                .Select(p => new PartDto
                {
                    PartId = p.PartId,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    Sku = p.Sku,
                    PartName = p.PartName,
                    Description = p.Description,
                    CostPrice = p.CostPrice,
                    SalePrice = p.SalePrice,
                    CurrentStock = p.CurrentStock,
                    ReorderLevel = p.ReorderLevel,
                    Unit = p.Unit,
                    RackLocation = p.RackLocation,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/parts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PartDto>> GetById(int id)
        {
            var p = await _context.Parts
                .AsNoTracking()
                .Where(x => x.PartId == id)
                .Select(p => new PartDto
                {
                    PartId = p.PartId,
                    CategoryId = p.CategoryId,
                    SupplierId = p.SupplierId,
                    Sku = p.Sku,
                    PartName = p.PartName,
                    Description = p.Description,
                    CostPrice = p.CostPrice,
                    SalePrice = p.SalePrice,
                    CurrentStock = p.CurrentStock,
                    ReorderLevel = p.ReorderLevel,
                    Unit = p.Unit,
                    RackLocation = p.RackLocation,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

            if (p == null)
                return NotFound();

            return Ok(p);
        }

        // POST: api/parts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePartDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var part = new Part
            {
                CategoryId = dto.CategoryId,
                SupplierId = dto.SupplierId,
                Sku = dto.Sku,
                PartName = dto.PartName,
                Description = dto.Description,
                CostPrice = dto.CostPrice,
                SalePrice = dto.SalePrice,
                CurrentStock = dto.CurrentStock,
                ReorderLevel = dto.ReorderLevel,
                Unit = dto.Unit,
                RackLocation = dto.RackLocation,
                IsActive = dto.IsActive
            };

            // Remove navigation-related validation expectations by using TryValidateModel
            TryValidateModel(part);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Business rules: Category and Supplier required
            if (!part.CategoryId.HasValue)
            {
                ModelState.AddModelError("CategoryId", "Category is required.");
                return ValidationProblem(ModelState);
            }
            if (!part.SupplierId.HasValue)
            {
                ModelState.AddModelError("SupplierId", "Supplier is required.");
                return ValidationProblem(ModelState);
            }

            // SKU uniqueness check
            if (!string.IsNullOrWhiteSpace(part.Sku) && await _context.Parts.AnyAsync(x => x.Sku == part.Sku))
            {
                ModelState.AddModelError("Sku", "SKU already exists.");
                return ValidationProblem(ModelState);
            }

            try
            {
                _context.Parts.Add(part);
                await _context.SaveChangesAsync();

                var result = new PartDto
                {
                    PartId = part.PartId,
                    CategoryId = part.CategoryId,
                    SupplierId = part.SupplierId,
                    Sku = part.Sku,
                    PartName = part.PartName,
                    Description = part.Description,
                    CostPrice = part.CostPrice,
                    SalePrice = part.SalePrice,
                    CurrentStock = part.CurrentStock,
                    ReorderLevel = part.ReorderLevel,
                    Unit = part.Unit,
                    RackLocation = part.RackLocation,
                    IsActive = part.IsActive
                };

                return CreatedAtAction(nameof(GetById), new { id = part.PartId }, result);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while creating the part." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT: api/parts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePartDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound();

            part.CategoryId = dto.CategoryId;
            part.SupplierId = dto.SupplierId;
            part.Sku = dto.Sku;
            part.PartName = dto.PartName;
            part.Description = dto.Description;
            part.CostPrice = dto.CostPrice;
            part.SalePrice = dto.SalePrice;
            part.CurrentStock = dto.CurrentStock;
            part.ReorderLevel = dto.ReorderLevel;
            part.Unit = dto.Unit;
            part.RackLocation = dto.RackLocation;
            part.IsActive = dto.IsActive;

            TryValidateModel(part);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (!part.CategoryId.HasValue)
            {
                ModelState.AddModelError("CategoryId", "Category is required.");
                return ValidationProblem(ModelState);
            }
            if (!part.SupplierId.HasValue)
            {
                ModelState.AddModelError("SupplierId", "Supplier is required.");
                return ValidationProblem(ModelState);
            }

            if (!string.IsNullOrWhiteSpace(part.Sku) && await _context.Parts.AnyAsync(x => x.Sku == part.Sku && x.PartId != part.PartId))
            {
                ModelState.AddModelError("Sku", "SKU already exists.");
                return ValidationProblem(ModelState);
            }

            try
            {
                _context.Entry(part).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Parts.AnyAsync(e => e.PartId == id))
                    return NotFound();
                return Conflict(new { message = "A concurrency error occurred while updating the part." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while updating the part." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE: api/parts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var part = await _context.Parts
                .Include(p => p.JobPartItems)
                .Include(p => p.PurchaseOrderItems)
                .Include(p => p.StockTransactions)
                .Include(p => p.LowStockAlerts)
                .FirstOrDefaultAsync(p => p.PartId == id);

            if (part == null)
                return NotFound();

            var hasJobs = part.JobPartItems != null && part.JobPartItems.Any();
            var hasPoItems = part.PurchaseOrderItems != null && part.PurchaseOrderItems.Any();
            var hasStockTx = part.StockTransactions != null && part.StockTransactions.Any();
            var hasAlerts = part.LowStockAlerts != null && part.LowStockAlerts.Any();

            if (hasJobs || hasPoItems || hasStockTx || hasAlerts)
            {
                return Conflict(new { message = "Cannot delete this part because it is referenced by existing records (jobs, purchase orders, stock transactions or alerts). Deactivate it instead." });
            }

            try
            {
                _context.Parts.Remove(part);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while deleting the part." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DTOs
        public class PartDto
        {
            public int PartId { get; set; }
            public int? CategoryId { get; set; }
            public int? SupplierId { get; set; }
            public string? Sku { get; set; }
            public string PartName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal CostPrice { get; set; }
            public decimal SalePrice { get; set; }
            public int? CurrentStock { get; set; }
            public int? ReorderLevel { get; set; }
            public string? Unit { get; set; }
            public string? RackLocation { get; set; }
            public bool? IsActive { get; set; }
        }

        public class CreatePartDto
        {
            public int? CategoryId { get; set; }
            public int? SupplierId { get; set; }
            public string? Sku { get; set; }
            public string PartName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal CostPrice { get; set; }
            public decimal SalePrice { get; set; }
            public int? CurrentStock { get; set; }
            public int? ReorderLevel { get; set; }
            public string? Unit { get; set; }
            public string? RackLocation { get; set; }
            public bool? IsActive { get; set; }
        }

        public class UpdatePartDto
        {
            public int? CategoryId { get; set; }
            public int? SupplierId { get; set; }
            public string? Sku { get; set; }
            public string PartName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal CostPrice { get; set; }
            public decimal SalePrice { get; set; }
            public int? CurrentStock { get; set; }
            public int? ReorderLevel { get; set; }
            public string? Unit { get; set; }
            public string? RackLocation { get; set; }
            public bool? IsActive { get; set; }
        }
    }
}
