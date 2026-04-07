using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using APIFurnitureCRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APIFurnitureCRM.Utils; // Добавляем using для AuthHelper

namespace APIFurnitureCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Staff>>> GetStaff()
        {
            // Возвращаем только сотрудников, чьи учетные записи активны (или у кого учетной записи пока нет)
            return await _context.Staff
                .Include(s => s.UserAccount)
                .Where(s => s.UserAccount == null || s.UserAccount.IsActive)
                .ToListAsync();
        }

        /// <summary>
        /// Возвращает уволенных (неактивных) сотрудников.
        /// Считаем уволенным сотрудника, у которого есть учетная запись с IsActive = false.
        /// </summary>
        [HttpGet("fired")]
        public async Task<ActionResult<IEnumerable<Staff>>> GetFiredStaff()
        {
            return await _context.Staff
                .Include(s => s.UserAccount)
                .Where(s => s.UserAccount != null && !s.UserAccount.IsActive)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Staff>> GetStaff(int id)
        {
            var staff = await _context.Staff.FindAsync(id);

            if (staff == null)
            {
                return NotFound();
            }

            return staff;
        }

        [HttpPost]
        public async Task<ActionResult<Staff>> PostStaff(Staff staff)
        {
            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            // Автоматическое создание учетной записи пользователя для нового сотрудника
            // Генерируем логин до тех пор, пока не найдём уникальное значение
            string generatedUsername;
            do
            {
                generatedUsername = AuthHelper.GenerateRandomUsername(staff.FullName);
            } while (await _context.UserAccounts.AnyAsync(ua => ua.Username == generatedUsername));

            var userAccount = new UserAccount
            {
                StaffId = staff.StaffId,
                Username = generatedUsername,
                Password = AuthHelper.GenerateRandomPassword() // Пока без хеширования
            };
            _context.UserAccounts.Add(userAccount);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStaff), new { id = staff.StaffId }, staff);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutStaff(int id, Staff staff)
        {
            if (id != staff.StaffId)
            {
                return BadRequest();
            }

            var existingStaff = await _context.Staff.Include(s => s.UserAccount).FirstOrDefaultAsync(s => s.StaffId == id);

            if (existingStaff == null)
            {
                return NotFound();
            }

            // Если при редактировании сотрудника передан логин, проверяем его уникальность
            if (staff.UserAccount != null && !string.IsNullOrWhiteSpace(staff.UserAccount.Username))
            {
                var newUsername = staff.UserAccount.Username;

                bool usernameExists = await _context.UserAccounts
                    .AnyAsync(ua => ua.Username == newUsername && ua.StaffId != existingStaff.StaffId);

                if (usernameExists)
                {
                    // 409 Conflict — логин уже используется другой учетной записью
                    return Conflict("Пользователь с таким логином уже существует. Пожалуйста, укажите другой логин.");
                }
            }

            // Обновляем основные свойства Staff
            existingStaff.FullName = staff.FullName;
            existingStaff.Position = staff.Position;

            // Обрабатываем UserAccount
            if (staff.UserAccount != null)
            {
                if (existingStaff.UserAccount != null)
                {
                    // Обновляем существующий UserAccount
                    existingStaff.UserAccount.Username = staff.UserAccount.Username;
                    existingStaff.UserAccount.Password = staff.UserAccount.Password; // В идеале здесь должно быть хеширование
                }
                else
                {
                    // Создаем новый UserAccount, если его не было
                    existingStaff.UserAccount = new UserAccount
                    {
                        StaffId = existingStaff.StaffId,
                        Username = staff.UserAccount.Username,
                        Password = staff.UserAccount.Password // В идеале здесь должно быть хеширование
                    };
                    _context.UserAccounts.Add(existingStaff.UserAccount);
                }
            } 
            else if (existingStaff.UserAccount != null)
            {
                // Если в входящем объекте UserAccount отсутствует, а в существующем он есть, удаляем его.
                _context.UserAccounts.Remove(existingStaff.UserAccount);
                existingStaff.UserAccount = null; // Отвязываем от Staff
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Staff.Any(e => e.StaffId == id))
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
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.UserAccount)
                .FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null)
            {
                return NotFound();
            }

            // Вместо физического удаления делаем "увольнение":
            // помечаем учетную запись как неактивную. Сотрудник исчезнет из списка,
            // так как GetStaff фильтрует по IsActive.
            if (staff.UserAccount != null)
            {
                staff.UserAccount.IsActive = false;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqliteException sqliteEx)
                {
                    if (sqliteEx.Message.Contains("STAFF_HAS_ACTIVE_CUSTOMER_ORDERS"))
                    {
                        return Conflict("Нельзя уволить сотрудника: у него есть незавершённые заказы клиентов.");
                    }
                    if (sqliteEx.Message.Contains("STAFF_HAS_ACTIVE_MATERIAL_ORDERS"))
                    {
                        return Conflict("Нельзя уволить сотрудника: у него есть незавершённые заказы на закупку сырья.");
                    }
                }
                throw;
            }

            return NoContent();
        }

        /// <summary>
        /// Восстановление (возврат в штат) уволенного сотрудника.
        /// Если учетной записи нет — создаём новую с уникальным логином и случайным паролем.
        /// Если учетная запись есть, просто помечаем её как активную.
        /// </summary>
        [HttpPut("{id}/reinstate")]
        public async Task<IActionResult> ReinstateStaff(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.UserAccount)
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null)
            {
                return NotFound("Сотрудник не найден.");
            }

            if (staff.UserAccount != null && staff.UserAccount.IsActive)
            {
                return BadRequest("Сотрудник уже находится в штате.");
            }

            if (staff.UserAccount == null)
            {
                // Создаём новую активную учетную запись для сотрудника
                string generatedUsername;
                do
                {
                    generatedUsername = AuthHelper.GenerateRandomUsername(staff.FullName);
                } while (await _context.UserAccounts.AnyAsync(ua => ua.Username == generatedUsername));

                var userAccount = new UserAccount
                {
                    StaffId = staff.StaffId,
                    Username = generatedUsername,
                    Password = AuthHelper.GenerateRandomPassword(),
                    IsActive = true
                };

                staff.UserAccount = userAccount;
                _context.UserAccounts.Add(userAccount);
            }
            else
            {
                // Просто возвращаем существующую учетную запись в активное состояние
                staff.UserAccount.IsActive = true;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
