using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using APIFurnitureCRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System; // Added for Console.WriteLine

namespace APIFurnitureCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Login) || string.IsNullOrWhiteSpace(loginRequest.Password))
            {
                return BadRequest("Логин и пароль не могут быть пустыми.");
            }

            // Находим учетную запись пользователя по логину
            var userAccount = await _context.UserAccounts
                                            .Include(ua => ua.Staff) // Включаем данные о сотруднике
                                            .FirstOrDefaultAsync(ua => ua.Username == loginRequest.Login);

            if (userAccount == null)
            {
                return NotFound("Пользователь с таким логином не найден.");
            }

            // TODO: В реальном приложении здесь должна быть реализована проверка хешированного пароля.
            // Для простоты примера, пока сравниваем пароли напрямую.
            if (userAccount.Password != loginRequest.Password)
            {
                return Unauthorized("Неверный пароль.");
            }

            // Проверяем, активна ли учетная запись (сотрудник не уволен)
            if (!userAccount.IsActive)
            {
                // 403 Forbidden с понятным сообщением
                return StatusCode(403, "Вход невозможен: учетная запись сотрудника деактивирована. Обратитесь к руководству.");
            }

            // Если аутентификация успешна, возвращаем данные о сотруднике и его должности
            var authenticatedUser = new AuthenticatedUser
            {
                ID_сотрудника = userAccount.StaffId,
                ФИО = userAccount.Staff?.FullName ?? "Неизвестно", // Если Staff null, используем "Неизвестно"
                Должность = userAccount.Staff?.Position ?? "Неизвестно" // Если Staff null, используем "Неизвестно"
            };

            return Ok(authenticatedUser);
        }
    }

    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthenticatedUser
    {
        public int ID_сотрудника { get; set; }
        public string ФИО { get; set; } = string.Empty;
        public string Должность { get; set; } = string.Empty;
    }
}
