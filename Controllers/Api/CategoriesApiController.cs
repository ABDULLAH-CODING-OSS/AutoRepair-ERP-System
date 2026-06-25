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
    public class CategoriesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesApiController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var list = await _context.Categories
                .AsNoTracking()
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var c = await _context.Categories
                .AsNoTracking()
                .Where(x => x.CategoryId == id)
                .Select(c => new CategoryDto
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Description = c.Description
                })
                .FirstOrDefaultAsync();

            if (c == null)
                return NotFound();

            return Ok(c);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var category = new Category
            {
                CategoryName = dto.CategoryName,
                Description = dto.Description
            };

            TryValidateModel(category);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var result = new CategoryDto
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName,
                    Description = category.Description
                };

                return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, result);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while creating the category." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT: api/categories/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            category.CategoryName = dto.CategoryName;
            category.Description = dto.Description;

            TryValidateModel(category);
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                _context.Entry(category).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categories.AnyAsync(e => e.CategoryId == id))
                    return NotFound();
                return Conflict(new { message = "A concurrency error occurred while updating the category." });
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while updating the category." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE: api/categories/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A database error occurred while deleting the category." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DTOs
        public class CategoryDto
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class CreateCategoryDto
        {
            public string CategoryName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class UpdateCategoryDto
        {
            public string CategoryName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }
    }
}
