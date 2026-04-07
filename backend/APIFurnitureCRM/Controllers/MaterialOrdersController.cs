using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using APIFurnitureCRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIFurnitureCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialOrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialOrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/materialorders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaterialOrder>>> GetMaterialOrders()
        {
            return await _context.MaterialOrders.ToListAsync();
        }

        // GET: api/materialorders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MaterialOrder>> GetMaterialOrder(int id)
        {
            var order = await _context.MaterialOrders.FindAsync(id);
            if (order == null)
                return NotFound();
            return order;
        }

        // POST: api/materialorders
        [HttpPost]
        public async Task<ActionResult<MaterialOrder>> PostMaterialOrder(MaterialOrder order)
        {
            var logger = HttpContext.RequestServices.GetService(typeof(ILogger<MaterialOrdersController>)) as ILogger;

            // Id автогенерируется. Дату заказа НЕ устанавливаем в коде — её ставит сама БД
            // через DEFAULT (DATETIME('now','localtime')) для столбца Дата_заказа.
            if (string.IsNullOrEmpty(order.Status)) order.Status = "Ожидает поставки";
            
            logger?.LogInformation("Creating MaterialOrder: OrderDate={OrderDate}, Status={Status}, Supplier={Supplier}, MaterialId={MaterialId}, Quantity={Quantity}, StaffId={StaffId}", 
                order.OrderDate, order.Status, order.Supplier, order.MaterialId, order.Quantity, order.StaffId);
            
            try
            {
                _context.MaterialOrders.Add(order);
                await _context.SaveChangesAsync();
                logger?.LogInformation("MaterialOrder created successfully with ID={MaterialOrderId}", order.MaterialOrderId);
            }
            catch (DbUpdateException ex)
            {
                // Обрабатываем ошибки триггеров/ограничений, чтобы вернуть понятное сообщение клиенту
                if (ex.InnerException is SqliteException sqliteEx)
                {
                    // Ошибка от триггера: только кладовщик может оформлять заказ сырья
                    if (sqliteEx.Message.Contains("ONLY_STOREKEEPER_CAN_CREATE_MATERIAL_ORDER"))
                    {
                        logger?.LogWarning("Attempt to create material order by non-warehouse staff. StaffId={StaffId}", order.StaffId);
                        return BadRequest("Оформлять заказ сырья может только сотрудник с должностью \"Кладовщик\".");
                    }
                    // Ошибка от триггера проверки допустимости смены статуса заказа сырья
                    if (sqliteEx.Message.Contains("INVALID_MATERIAL_ORDER_STATUS_TRANSITION"))
                    {
                        logger?.LogWarning("Invalid material order status transition on create. Status={Status}", order.Status);
                        return BadRequest("Недопустимое изменение статуса заказа сырья. Разрешены переходы: \"Ожидает поставки\" → \"Доставлен\" или \"Отменен\"; из статусов \"Доставлен\" и \"Отменен\" переходы запрещены.");
                    }
                }

                logger?.LogError(ex, "Error creating MaterialOrder.");
                throw;
            }

            return CreatedAtAction(nameof(GetMaterialOrder), new { id = order.MaterialOrderId }, order);
        }

        // PUT: api/materialorders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaterialOrder(int id, MaterialOrder order)
        {
            if (id != order.MaterialOrderId)
                return BadRequest();

            // Убедимся, что такой заказ существует
            var exists = await _context.MaterialOrders.AnyAsync(x => x.MaterialOrderId == id);
            if (!exists)
                return NotFound();

            _context.Entry(order).State = EntityState.Modified;
            // Датой управляет БД/триггер, не перезаписываем её с клиента
            _context.Entry(order).Property(o => o.OrderDate).IsModified = false;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.MaterialOrders.Any(e => e.MaterialOrderId == id))
                    return NotFound();
                else
                    throw;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqliteException sqliteEx)
                {
                    if (sqliteEx.Message.Contains("MATERIAL_NOT_FOUND_FOR_MATERIAL_ORDER"))
                    {
                        return Conflict("Материал для данной закупки не найден в справочнике сырья.");
                    }
                    if (sqliteEx.Message.Contains("INVALID_MATERIAL_ORDER_STATUS_TRANSITION"))
                    {
                        return BadRequest("Недопустимое изменение статуса заказа сырья. Разрешены переходы: \"Ожидает поставки\" → \"Доставлен\" или \"Отменен\"; из статусов \"Доставлен\" и \"Отменен\" переходы запрещены.");
                    }
                }
                throw;
            }
            return NoContent();
        }

        // DELETE: api/materialorders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterialOrder(int id)
        {
            var order = await _context.MaterialOrders.FindAsync(id);
            if (order == null)
                return NotFound();
            _context.MaterialOrders.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
