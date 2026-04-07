using Microsoft.AspNetCore.Mvc;
using APIFurnitureCRM.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APIFurnitureCRM.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        public class ProductionReportItem
        {
            public int OrderId { get; set; }
            public DateTime OrderDate { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Status { get; set; } = string.Empty;
            public int TotalPrice { get; set; }
        }

        public class ProductionReportResult
        {
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public int TotalOrders { get; set; }
            public int InProcessingCount { get; set; }
            public int InProductionCount { get; set; }
            public int CompletedCount { get; set; }
            public int CancelledCount { get; set; }
            public int TotalRevenue { get; set; }
            public List<ProductionReportItem> Orders { get; set; } = new();
        }

        public class ManagerProductionSummary
        {
            public int StaffId { get; set; }
            public string StaffFullName { get; set; } = string.Empty;
            public int TotalOrders { get; set; }
            public int CompletedOrders { get; set; }
            public int CancelledOrders { get; set; }
            public int TotalRevenue { get; set; }
        }

        public class ProductProductionSummary
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int TotalOrders { get; set; }
            public int TotalQuantity { get; set; }
            public int CompletedOrders { get; set; }
            public int TotalRevenue { get; set; }
        }

        public class MaterialNeedItem
        {
            public int MaterialId { get; set; }
            public string MaterialName { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public int QuantityAvailable { get; set; }
            public int RequiredQuantity { get; set; }
            // Остаток: > 0, если материала хватает; < 0, если есть дефицит
            public int Deficit => QuantityAvailable - RequiredQuantity;
        }

        public class MaterialUsageItem
        {
            public int MaterialId { get; set; }
            public string MaterialName { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public int TotalRequired { get; set; }
        }

        // GET: api/reports/production?from=2025-01-01&to=2025-01-31
        [HttpGet("production")]
        public async Task<ActionResult<ProductionReportResult>> GetProductionReport(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.Orders
                .Include(o => o.Product)
                .AsQueryable();

            if (from.HasValue)
            {
                // Включительное сравнение: OrderDate >= начало дня fromDate
                // Нормализуем дату к началу дня в локальном времени
                var fromDate = from.Value.Kind == DateTimeKind.Utc 
                    ? from.Value.ToLocalTime().Date 
                    : from.Value.Date; // Начало дня (00:00:00)
                query = query.Where(o => o.OrderDate >= fromDate);
            }

            if (to.HasValue)
            {
                // Включительное сравнение: OrderDate <= конец дня toDate
                // Нормализуем дату и используем конец дня для включительного сравнения
                var toDate = to.Value.Kind == DateTimeKind.Utc 
                    ? to.Value.ToLocalTime().Date 
                    : to.Value.Date;
                var toDateEnd = toDate.AddDays(1).AddTicks(-1); // Конец дня (23:59:59.9999999)
                query = query.Where(o => o.OrderDate <= toDateEnd);
            }

            var orders = await query
                .OrderBy(o => o.OrderDate)
                .ToListAsync();

            var items = orders.Select(o => new ProductionReportItem
            {
                OrderId = o.OrderId,
                OrderDate = o.OrderDate,
                ProductName = o.Product?.Name ?? $"Товар {o.ProductId}",
                Quantity = o.Quantity,
                Status = o.Status,
                TotalPrice = o.TotalPrice
            }).ToList();

            var result = new ProductionReportResult
            {
                FromDate = from?.Date,
                ToDate = to?.Date,
                TotalOrders = items.Count,
                InProcessingCount = items.Count(o => o.Status == "В обработке"),
                InProductionCount = items.Count(o => o.Status == "В производстве"),
                CompletedCount = items.Count(o => o.Status == "Выполнен"),
                CancelledCount = items.Count(o => o.Status == "Отменен"),
                TotalRevenue = items
                    .Where(o => o.Status == "Выполнен")
                    .Sum(o => o.TotalPrice),
                Orders = items
            };

            return Ok(result);
        }

        // Агрегация по менеджерам: сколько заказов, сколько выполнено/отменено, выручка
        // GET: api/reports/production/by-manager?from=...&to=...
        [HttpGet("production/by-manager")]
        public async Task<ActionResult<IEnumerable<ManagerProductionSummary>>> GetProductionByManager(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.Orders
                .Include(o => o.Staff)
                .AsQueryable();

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(o => o.OrderDate >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date;
                query = query.Where(o => o.OrderDate <= toDate);
            }

            var grouped = await query
                .GroupBy(o => new { o.StaffId, o.Staff.FullName })
                .Select(g => new ManagerProductionSummary
                {
                    StaffId = g.Key.StaffId,
                    StaffFullName = g.Key.FullName ?? $"Сотрудник {g.Key.StaffId}",
                    TotalOrders = g.Count(),
                    CompletedOrders = g.Count(o => o.Status == "Выполнен"),
                    CancelledOrders = g.Count(o => o.Status == "Отменен"),
                    TotalRevenue = g.Where(o => o.Status == "Выполнен").Sum(o => o.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            return Ok(grouped);
        }

        // Агрегация по номенклатуре: сколько заказов, количество, выполненные, выручка
        // GET: api/reports/production/by-product?from=...&to=...&staffId=...
        [HttpGet("production/by-product")]
        public async Task<ActionResult<IEnumerable<ProductProductionSummary>>> GetProductionByProduct(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? staffId)
        {
            var query = _context.Orders
                .Include(o => o.Product)
                .AsQueryable();

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(o => o.OrderDate >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date;
                query = query.Where(o => o.OrderDate <= toDate);
            }

            if (staffId.HasValue)
            {
                query = query.Where(o => o.StaffId == staffId.Value);
            }

            var grouped = await query
                .GroupBy(o => new { o.ProductId, o.Product.Name })
                .Select(g => new ProductProductionSummary
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name ?? $"Товар {g.Key.ProductId}",
                    TotalOrders = g.Count(),
                    TotalQuantity = g.Sum(o => o.Quantity),
                    CompletedOrders = g.Count(o => o.Status == "Выполнен"),
                    TotalRevenue = g.Where(o => o.Status == "Выполнен").Sum(o => o.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            return Ok(grouped);
        }

        // Анализ потребности в материалах на основе заказов и спецификаций
        // GET: api/reports/material-needs?from=...&to=...
        [HttpGet("material-needs")]
        public async Task<ActionResult<IEnumerable<MaterialNeedItem>>> GetMaterialNeeds(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            // Берём заказы в статусе "В обработке".
            // Заказы "В производстве" уже списали сырьё триггером, поэтому повторно их не учитываем,
            // иначе будет двойной учёт потребности.
            var ordersQuery = _context.Orders.AsQueryable();

            ordersQuery = ordersQuery.Where(o => o.Status == "В обработке");

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date;
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= toDate);
            }

            // Джойним заказы с спецификацией и материалами
            var query = from o in ordersQuery
                        join s in _context.Specifications on o.ProductId equals s.ProductId
                        join m in _context.Materials on s.MaterialId equals m.MaterialId
                        select new
                        {
                            m.MaterialId,
                            m.MaterialName,
                            m.Unit,
                            m.QuantityAvailable,
                            OrderQuantity = o.Quantity,
                            PerProductMaterialQuantity = s.Quantity
                        };

            // Переключаемся на LINQ to Objects для группировки, так как провайдер БД
            // не всегда умеет переводить сложные GroupBy + вычисления
            var data = await query.ToListAsync();

            var grouped = data
                .GroupBy(x => new { x.MaterialId, x.MaterialName, x.Unit, x.QuantityAvailable })
                .Select(g => new MaterialNeedItem
                {
                    MaterialId = g.Key.MaterialId,
                    MaterialName = g.Key.MaterialName,
                    Unit = g.Key.Unit,
                    QuantityAvailable = g.Key.QuantityAvailable,
                    RequiredQuantity = g.Sum(x => x.OrderQuantity * x.PerProductMaterialQuantity)
                })
                // Сначала показываем материалы с наибольшим дефицитом (самый маленький остаток)
                .OrderBy(x => x.Deficit)
                .ToList();

            return Ok(grouped);
        }

        // Совокупное потребление материалов по заказам за последние 3 месяца
        // GET: api/reports/material-usage
        [HttpGet("material-usage")]
        public async Task<ActionResult<IEnumerable<MaterialUsageItem>>> GetMaterialUsage()
        {
            // Берём заказы за последние 3 календарных месяца относительно сегодняшней даты.
            var threeMonthsAgo = DateTime.Today.AddMonths(-3);
            var today = DateTime.Today;

            var query = from o in _context.Orders
                        where o.OrderDate >= threeMonthsAgo && o.OrderDate <= today
                        join s in _context.Specifications on o.ProductId equals s.ProductId
                        join m in _context.Materials on s.MaterialId equals m.MaterialId
                        select new
                        {
                            m.MaterialId,
                            m.MaterialName,
                            m.Unit,
                            OrderQuantity = o.Quantity,
                            PerProductMaterialQuantity = s.Quantity
                        };

            var data = await query.ToListAsync();

            var grouped = data
                .GroupBy(x => new { x.MaterialId, x.MaterialName, x.Unit })
                .Select(g => new MaterialUsageItem
                {
                    MaterialId = g.Key.MaterialId,
                    MaterialName = g.Key.MaterialName,
                    Unit = g.Key.Unit,
                    TotalRequired = g.Sum(x => x.OrderQuantity * x.PerProductMaterialQuantity)
                })
                .OrderByDescending(x => x.TotalRequired)
                .ToList();

            return Ok(grouped);
        }
    }
}


