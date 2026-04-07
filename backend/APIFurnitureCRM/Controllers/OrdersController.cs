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
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Staff)
                .Include(o => o.Client)
                .ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.Staff)
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            // Дату оформления и дату выполнения при создании не принимаем от клиента:
            // - дата оформления ставится БД по DEFAULT (DATE('now'))
            // - дата выполнения ставится триггером при переводе статуса в "Выполнен"
            order.OrderDate = default;
            order.CompletionDate = null;

            _context.Orders.Add(order);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Обрабатываем ошибку из триггера: только менеджер может оформлять заказ номенклатуры
                if (ex.InnerException is SqliteException sqliteEx &&
                    sqliteEx.Message.Contains("ONLY_MANAGER_CAN_CREATE_ORDER"))
                {
                    return BadRequest("Оформлять заказ номенклатуры может только сотрудник с должностью \"Менеджер\".");
                }
                throw;
            }

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }

        // PUT: api/Orders/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.OrderId)
                return BadRequest();

            // Загружаем текущее состояние из БД, чтобы не дать клиенту менять
            // даты и итоговую стоимость напрямую (их контролирует БД/триггеры)
            var existing = await _context.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (existing == null)
                return NotFound();

            // Фиксируем даты и итоговую стоимость: они остаются теми, что в БД.
            // Пересчёт TotalPrice выполняется триггером в БД при изменении товара/количества.
            order.OrderDate = existing.OrderDate;
            order.CompletionDate = existing.CompletionDate;
            order.TotalPrice = existing.TotalPrice;

            _context.Entry(order).State = EntityState.Modified;
            // Явно запрещаем менять дату оформления и итоговую стоимость с клиента
            _context.Entry(order).Property(o => o.OrderDate).IsModified = false;
            _context.Entry(order).Property(o => o.TotalPrice).IsModified = false;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                    return NotFound();
                else
                    throw;
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqliteException sqliteEx)
                {
                    // Обрабатываем ошибку из триггера проверки сырья
                    if (sqliteEx.Message.Contains("NOT_ENOUGH_MATERIALS_FOR_ORDER"))
                    {
                        return Conflict("Недостаточно сырья на складе для перевода заказа в производство.");
                    }
                    // Ошибка из триггера проверки допустимости смены статуса заказа
                    if (sqliteEx.Message.Contains("INVALID_ORDER_STATUS_TRANSITION"))
                    {
                        return BadRequest("Недопустимое изменение статуса заказа. Разрешены переходы: \"В обработке\" → \"В производстве\" или \"Отменен\"; \"В производстве\" → \"Выполнен\" или \"Отменен\"; из статусов \"Выполнен\" и \"Отменен\" переходы запрещены.");
                    }
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // СПИСАНИЕ СЫРЬЯ при переводе заказа в производство
        [HttpPost("{id}/produce")]
        public async Task<IActionResult> StartProduction(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound($"Заказ с id={id} не найден.");

            // Переводим заказ в статус "В производстве".
            // Триггер в БД проверит наличие сырья и спишет его,
            // либо отменит операцию с ошибкой NOT_ENOUGH_MATERIALS_FOR_ORDER.
            if (order.Status != "В обработке")
                return BadRequest("Перевести в производство можно только заказ со статусом \"В обработке\".");

            order.Status = "В производстве";

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Заказ переведен в производство, материалы списаны со склада.");
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqliteException sqliteEx &&
                    sqliteEx.Message.Contains("NOT_ENOUGH_MATERIALS_FOR_ORDER"))
                {
                    return Conflict("Недостаточно сырья на складе для перевода заказа в производство.");
                }
                throw;
            }
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
