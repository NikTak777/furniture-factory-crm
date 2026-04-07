using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using APIFurnitureCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace APIFurnitureCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IEnumerable<Client>> Get() => await _context.Clients.ToListAsync();

        [HttpPost]
        public async Task<IActionResult> Add(Client client)
        {
            // Базовая валидация: все поля должны быть заполнены
            if (string.IsNullOrWhiteSpace(client.FullName) ||
                client.Phone <= 0 ||
                string.IsNullOrWhiteSpace(client.Email) ||
                string.IsNullOrWhiteSpace(client.Address))
            {
                return BadRequest("Все поля клиента (ФИО, телефон, Email, адрес) должны быть заполнены.");
            }

            // Проверка уникальности телефона и Email
            if (await _context.Clients.AnyAsync(c => c.Phone == client.Phone))
            {
                return Conflict("Клиент с таким номером телефона уже существует.");
            }

            if (await _context.Clients.AnyAsync(c => c.Email == client.Email))
            {
                return Conflict("Клиент с таким Email уже существует.");
            }

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return Ok(client);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Client client)
        {
            if (id != client.Id)
                return BadRequest();

            // Базовая валидация: все поля должны быть заполнены
            if (string.IsNullOrWhiteSpace(client.FullName) ||
                client.Phone <= 0 ||
                string.IsNullOrWhiteSpace(client.Email) ||
                string.IsNullOrWhiteSpace(client.Address))
            {
                return BadRequest("Все поля клиента (ФИО, телефон, Email, адрес) должны быть заполнены.");
            }

            // Проверка уникальности телефона и Email (с исключением текущего клиента)
            if (await _context.Clients.AnyAsync(c => c.Id != id && c.Phone == client.Phone))
            {
                return Conflict("Клиент с таким номером телефона уже существует.");
            }

            if (await _context.Clients.AnyAsync(c => c.Id != id && c.Email == client.Email))
            {
                return Conflict("Клиент с таким Email уже существует.");
            }

            _context.Entry(client).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clients.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw; // Rethrow if it's not a NotFound issue
            }
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return NotFound();

            // Проверка на наличие связанных заказов перед удалением клиента
            bool hasOrders = await _context.Orders.AnyAsync(o => o.ClientId == id);
            if (hasOrders)
            {
                return Conflict("Нельзя удалить клиента, так как у него есть связанные заказы.");
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
