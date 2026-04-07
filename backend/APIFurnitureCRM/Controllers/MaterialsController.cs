using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using APIFurnitureCRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIFurnitureCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Materials
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials()
        {
            return await _context.Materials.ToListAsync();
        }

        // POST: api/Materials
        [HttpPost]
        public async Task<ActionResult<Material>> PostMaterial(Material material)
        {
            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMaterials), new { id = material.MaterialId }, material);
        }

        // PUT: api/Materials/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaterial(int id, Material material)
        {
            if (id != material.MaterialId)
            {
                return BadRequest();
            }

            _context.Entry(material).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Materials.Any(e => e.MaterialId == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Materials/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            // Проверка: есть ли заказы сырья со статусом "Ожидает поставки"
            bool hasPendingMaterialOrders = await _context.MaterialOrders
                .AnyAsync(mo => mo.MaterialId == id && mo.Status == "Ожидает поставки");
            if (hasPendingMaterialOrders)
            {
                return Conflict("Нельзя удалить сырьё, так как есть незавершенные заказы сырья.");
            }

            // Проверка: используется ли материал в спецификациях
            bool materialInSpecifications = await _context.Specifications.AnyAsync(s => s.MaterialId == id);
            if (materialInSpecifications)
            {
                return Conflict("Нельзя удалить сырьё, так как оно используется в спецификации номенклатуры.");
            }

            // Проверка: есть ли другие связанные заказы сырья (не "Ожидает поставки" и не использованные в спецификации)
            bool hasOtherMaterialOrders = await _context.MaterialOrders.AnyAsync(mo => mo.MaterialId == id);
            if (hasOtherMaterialOrders)
            {
                return Conflict("Нельзя удалить сырьё, так как оно уже использовалось в других заказах сырья.");
            }
            
            _context.Materials.Remove(material);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
