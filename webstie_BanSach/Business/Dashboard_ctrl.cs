using System;
using System.Collections.Generic;
using System.Linq;
using webstie_BanSach.Models;

namespace webstie_BanSach.Business
{
    public class Dashboard_ctrl
    {
        public class KpiDto
        {
            public int ProductCount { get; set; }
            public decimal Revenue { get; set; }
            public int OrderCount { get; set; }
            public int CustomerCount { get; set; }
        }

        public class RecentOrderDto
        {
            public long OrderId { get; set; }
            public string OrderCode { get; set; }
            public string BuyerName { get; set; }
            public string CreatedAt { get; set; }
            public decimal GrandTotal { get; set; }
            public string Status { get; set; }
        }

        public class StatItemDto
        {
            public string Name { get; set; }
            public int Value { get; set; }
            public int Percent { get; set; }
        }

        public class AlertItemDto
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int Stock { get; set; }
            public string Level { get; set; }
        }

        public class DashboardDto
        {
            public KpiDto Kpi { get; set; }
            public List<RecentOrderDto> RecentOrders { get; set; }
            public List<StatItemDto> TopCategories { get; set; }
            public List<AlertItemDto> LowStockBooks { get; set; }
        }

        public FunctResult<DashboardDto> GetDashboard(int days)
        {
            var result = new FunctResult<DashboardDto>();
            try
            {
                if (days != 7 && days != 30 && days != 90) days = 30;
                var fromUtc = DateTime.UtcNow.Date.AddDays(-(days - 1));

                using (var db = new DatabaseDataContext())
                {
                    var ordersInRange = db.Orders.Where(o => o.CreatedAtUtc >= fromUtc);

                    var kpi = new KpiDto
                    {
                        ProductCount = db.Books.Count(b => !b.IsDeleted),
                        Revenue = ordersInRange.Select(o => (decimal?)o.GrandTotal).Sum() ?? 0m,
                        OrderCount = ordersInRange.Count(),
                        CustomerCount = db.Users.Count(u => u.Role != null && u.Role.RoleName == "CUSTOMER")
                    };
                    if (kpi.CustomerCount == 0)
                    {
                        kpi.CustomerCount = db.Users.Count(); // fallback
                    }

                    var recentRaw = db.Orders
                        .OrderByDescending(o => o.CreatedAtUtc)
                        .ThenByDescending(o => o.OrderId)
                        .Take(8)
                        .Select(o => new
                        {
                            o.OrderId,
                            o.OrderCode,
                            o.BuyerName,
                            o.CreatedAtUtc,
                            GrandTotal = o.GrandTotal,
                            o.StatusCode
                        })
                        .ToList();

                    var recent = recentRaw.Select(o => new RecentOrderDto
                    {
                        OrderId = o.OrderId,
                        OrderCode = o.OrderCode,
                        BuyerName = o.BuyerName,
                        CreatedAt = o.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                        GrandTotal = o.GrandTotal ?? 0m,
                        Status = o.StatusCode
                    }).ToList();

                    var catStats = (from oi in db.OrderItems
                                    join o in db.Orders on oi.OrderId equals o.OrderId
                                    join b in db.Books on oi.BookId equals b.BookId
                                    join c in db.Categories on b.CategoryId equals c.CategoryId
                                    where o.CreatedAtUtc >= fromUtc && !b.IsDeleted && !c.IsDeleted
                                    group oi by new { c.CategoryId, c.Name } into g
                                    select new { g.Key.Name, Value = g.Sum(x => x.Quantity) })
                                    .OrderByDescending(x => x.Value)
                                    .Take(5)
                                    .ToList();

                    var maxCat = catStats.Any() ? catStats.Max(x => x.Value) : 0;
                    var topCategories = catStats.Select(x => new StatItemDto
                    {
                        Name = x.Name,
                        Value = x.Value,
                        Percent = maxCat > 0 ? (int)Math.Round(x.Value * 100.0 / maxCat) : 0
                    }).ToList();

                    var lowStockRaw = db.Books
                        .Where(b => !b.IsDeleted && b.Active && b.Stock <= 10)
                        .OrderBy(b => b.Stock)
                        .ThenBy(b => b.Title)
                        .Take(5)
                        .Select(b => new { b.BookId, b.Title, b.Stock })
                        .ToList();

                    var lowStock = lowStockRaw.Select(b => new AlertItemDto
                    {
                        Id = b.BookId,
                        Name = b.Title,
                        Stock = b.Stock,
                        Level = b.Stock <= 3 ? "danger" : (b.Stock <= 10 ? "warning" : "info")
                    }).ToList();

                    result.ErrCode = 1;
                    result.ErrDesc = "OK";
                    result.Data = new DashboardDto
                    {
                        Kpi = kpi,
                        RecentOrders = recent,
                        TopCategories = topCategories,
                        LowStockBooks = lowStock
                    };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = -1;
                result.ErrDesc = "Lỗi khi tải dashboard: " + ex.Message;
                result.Data = null;
            }
            return result;
        }
    }
}
