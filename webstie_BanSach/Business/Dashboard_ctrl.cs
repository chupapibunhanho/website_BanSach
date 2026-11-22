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

        public class TopBookDto
        {
            public long BookId { get; set; }
            public string Title { get; set; }
            public string AuthorName { get; set; }
            public string CoverUrl { get; set; }
            public decimal Price { get; set; }
            public int Sold { get; set; }
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
            public List<TopBookDto> TopBooks { get; set; }
            public List<AlertItemDto> LowStockBooks { get; set; }
        }

        public FunctResult<DashboardDto> GetDashboard(string range)
        {
            var result = new FunctResult<DashboardDto>();
            try
            {
                var todayUtc = DateTime.UtcNow.Date;
                DateTime fromUtc;
                DateTime? toUtcExclusive = null;
                switch ((range ?? "30").Trim().ToLower())
                {
                    case "today":
                        fromUtc = todayUtc;
                        toUtcExclusive = todayUtc.AddDays(1);
                        break;
                    case "yesterday":
                        fromUtc = todayUtc.AddDays(-1);
                        toUtcExclusive = todayUtc;
                        break;
                    case "7":
                        fromUtc = todayUtc.AddDays(-6);
                        break;
                    case "90":
                        fromUtc = todayUtc.AddDays(-89);
                        break;
                    default:
                        fromUtc = todayUtc.AddDays(-29);
                        break;
                }

                using (var db = new DatabaseDataContext())
                {
                    var ordersInRange = db.Orders.Where(o => o.CreatedAtUtc >= fromUtc);
                    if (toUtcExclusive.HasValue)
                        ordersInRange = ordersInRange.Where(o => o.CreatedAtUtc < toUtcExclusive.Value);

                    var customerKeys = ordersInRange
                        .Select(o => new { o.UserId, o.BuyerEmail })
                        .ToList();

                    var kpi = new KpiDto
                    {
                        ProductCount = db.Books.Count(b => !b.IsDeleted),
                        Revenue = ordersInRange.Select(o => (decimal?)o.GrandTotal).Sum() ?? 0m,
                        OrderCount = ordersInRange.Count(),
                        CustomerCount = customerKeys
                            .Select(x => x.UserId.HasValue ? "U:" + x.UserId.Value : "E:" + (x.BuyerEmail ?? "").Trim().ToLower())
                            .Where(x => x != "E:")
                            .Distinct()
                            .Count()
                    };

                    var recentRaw = ordersInRange
                        .OrderByDescending(o => o.CreatedAtUtc)
                        .ThenByDescending(o => o.OrderId)
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
                                    where !b.IsDeleted && !c.IsDeleted
                                    group oi by new { c.CategoryId, c.Name } into g
                                    select new { g.Key.Name, Value = g.Sum(x => x.Quantity) })
                                    .OrderByDescending(x => x.Value)
                                    .Take(10)
                                    .ToList();

                    var maxCat = catStats.Any() ? catStats.Max(x => x.Value) : 0;
                    var topCategories = catStats.Select(x => new StatItemDto
                    {
                        Name = x.Name,
                        Value = x.Value,
                        Percent = maxCat > 0 ? (int)Math.Round(x.Value * 100.0 / maxCat) : 0
                    }).ToList();

                    var topBookStats = (from oi in db.OrderItems
                                        join o in db.Orders on oi.OrderId equals o.OrderId
                                        join b in db.Books on oi.BookId equals b.BookId
                                        where !b.IsDeleted
                                        group oi by new { b.BookId, b.Title, b.CoverUrl, b.Price } into g
                                        select new
                                        {
                                            g.Key.BookId,
                                            g.Key.Title,
                                            g.Key.CoverUrl,
                                            g.Key.Price,
                                            Sold = g.Sum(x => x.Quantity)
                                        })
                                        .OrderByDescending(x => x.Sold)
                                        .Take(10)
                                        .ToList();

                    var topBookIds = topBookStats.Select(x => x.BookId).ToList();
                    var authorMap = (from ba in db.BookAuthors
                                     join a in db.Authors on ba.AuthorId equals a.AuthorId
                                     where topBookIds.Contains(ba.BookId)
                                     group a.AuthorName by ba.BookId into g
                                     select new { BookId = g.Key, AuthorName = g.OrderBy(n => n).FirstOrDefault() })
                                     .ToDictionary(x => x.BookId, x => x.AuthorName);

                    var maxSold = topBookStats.Any() ? topBookStats.Max(x => x.Sold) : 0;
                    var topBooks = topBookStats.Select(x => new TopBookDto
                    {
                        BookId = x.BookId,
                        Title = x.Title,
                        AuthorName = authorMap.ContainsKey(x.BookId) ? authorMap[x.BookId] : "",
                        CoverUrl = x.CoverUrl,
                        Price = x.Price,
                        Sold = x.Sold,
                        Percent = maxSold > 0 ? (int)Math.Round(x.Sold * 100.0 / maxSold) : 0
                    }).ToList();

                    var lowStockRaw = db.Books
                        .Where(b => !b.IsDeleted && b.Active && b.Stock < 15)
                        .OrderBy(b => b.Stock)
                        .ThenBy(b => b.Title)
                        .Select(b => new { b.BookId, b.Title, b.Stock })
                        .ToList();

                    var lowStock = lowStockRaw.Select(b => new AlertItemDto
                    {
                        Id = b.BookId,
                        Name = b.Title,
                        Stock = b.Stock,
                        Level = b.Stock <= 3 ? "danger" : (b.Stock < 15 ? "warning" : "info")
                    }).ToList();

                    result.ErrCode = 1;
                    result.ErrDesc = "OK";
                    result.Data = new DashboardDto
                    {
                        Kpi = kpi,
                        RecentOrders = recent,
                        TopCategories = topCategories,
                        TopBooks = topBooks,
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
