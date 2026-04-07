using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using APIFurnitureCRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Text.Json; // Добавляем этот using

namespace APIFurnitureCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NomenclatureController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NomenclatureController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            // Возвращаем только номенклатуру, которая сейчас производится (мягкое удаление через флаг IsProduced)
            return await _context.Products
                .Where(p => p.IsProduced)
                .Include(p => p.Specifications)
                .ThenInclude(s => s.Material)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает номенклатуру, снятую с производства (IsProduced = 0).
        /// </summary>
        [HttpGet("notproduced")]
        public async Task<ActionResult<IEnumerable<Product>>> GetNotProducedProducts()
        {
            return await _context.Products
                .Where(p => !p.IsProduced)
                .Include(p => p.Specifications)
                .ThenInclude(s => s.Material)
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            try
            {
                // Добавляем продукт
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Обработка спецификаций
                if (product.Specifications != null && product.Specifications.Any())
                {
                    foreach (var spec in product.Specifications)
                    {
                        if (spec.MaterialId == 0)
                            return BadRequest("MaterialId cannot be 0.");

                        var materialExists = await _context.Materials.AnyAsync(m => m.MaterialId == spec.MaterialId);
                        if (!materialExists)
                            return BadRequest($"Material with ID {spec.MaterialId} not found.");

                        spec.ProductId = product.ProductId;
                        spec.Material = null;
                        _context.Specifications.Attach(spec);
                    }

                    await _context.SaveChangesAsync();
                }

                return CreatedAtAction(nameof(GetProducts), new { id = product.ProductId }, product);
            }
            catch
            {
                // Возвращаем стандартную ошибку 500
                return StatusCode(500, "Произошла ошибка при сохранении продукта.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            var existingProduct = await _context.Products
                .Include(p => p.Specifications)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            // Обновляем основные свойства продукта
            _context.Entry(existingProduct).CurrentValues.SetValues(product);

            // Обработка спецификаций
            if (product.Specifications != null)
            {
                // Удаляем старые спецификации, которых нет в новой коллекции
                foreach (var existingSpec in existingProduct.Specifications.ToList())
                {
                    if (!product.Specifications.Any(s => s.MaterialId == existingSpec.MaterialId))
                    {
                        _context.Specifications.Remove(existingSpec);
                    }
                }

                // Добавляем новые и обновляем существующие спецификации
                foreach (var newSpec in product.Specifications)
                {
                    var existingSpec = existingProduct.Specifications
                        .FirstOrDefault(s => s.MaterialId == newSpec.MaterialId);

                    if (existingSpec == null)
                    {
                        // Новая спецификация
                        newSpec.ProductId = existingProduct.ProductId;
                        _context.Specifications.Add(newSpec);
                    }
                    else
                    {
                        // Обновляем существующую спецификацию (например, количество)
                        existingSpec.Quantity = newSpec.Quantity;
                    }
                }
            }
            else
            {
                // Если спецификации не были предоставлены, удаляем все существующие
                foreach (var existingSpec in existingProduct.Specifications.ToList())
                {
                    _context.Specifications.Remove(existingSpec);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.ProductId == id))
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Вместо физического удаления помечаем товар как снятый с производства
            product.IsProduced = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Возвращает ранее снятую номенклатуру обратно в производство (IsProduced = 1).
        /// </summary>
        [HttpPut("{id}/reinstate")]
        public async Task<IActionResult> ReinstateProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Номенклатура не найдена.");
            }

            if (product.IsProduced)
            {
                return BadRequest("Номенклатура уже находится в производстве.");
            }

            product.IsProduced = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
