using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using webstie_BanSach.Models;

namespace webstie_BanSach.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult BestSellersPartial()
        {
            using (var db = new DatabaseDataContext())
            {
                var ids = CustomerCache.TopBookIds(db);
                var books = db.Books.Where(b => ids.Contains(b.BookId) && !b.IsDeleted && b.Active).ToList();
                foreach (var book in books)
                {
                    book.SalesRank = ids.IndexOf(book.BookId) + 1;
                }
                ApplyRatings(db, books);
                ApplySoldCounts(db, books);
                return PartialView("Partials/_BestSellersList", books.OrderBy(b => b.SalesRank).ToList());
            }
        }

        [HttpGet]
        public ActionResult NewestPartial()
        {
            using (var db = new DatabaseDataContext())
            {
                var books = db.Books.Where(b => !b.IsDeleted && b.Active)
                    .OrderByDescending(b => b.CreatedAtUtc)
                    .ThenByDescending(b => b.BookId)
                    .Take(10).ToList();
                ApplyRatings(db, books);
                ApplySoldCounts(db, books);
                return PartialView("Partials/_NewestList", books);
            }
        }

        [HttpGet]
        public ActionResult CategoriesPartial()
        {
            using (var db = new DatabaseDataContext())
            {
                var categories = CustomerCache.Categories(db).Take(12).ToList();
                return PartialView("Partials/_CategoriesList", categories);
            }
        }

        private static void ApplyRatings(DatabaseDataContext db, List<Book> books)
        {
            if (books.Count == 0) return;
            var ids = books.Select(b => b.BookId).ToList();
            var ratings = db.BookReviews.Where(r => !r.IsDeleted && r.Rating > 0 && ids.Contains(r.BookId))
                .GroupBy(r => r.BookId)
                .Select(g => new { BookId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionary(r => r.BookId);
            foreach (var book in books)
            {
                if (ratings.TryGetValue(book.BookId, out var rating))
                {
                    book.AverageRating = rating.Average;
                    book.RatingCount = rating.Count;
                }
            }
        }

        private static void ApplySoldCounts(DatabaseDataContext db, List<Book> books)
        {
            if (books.Count == 0) return;
            var ids = books.Select(b => b.BookId).ToList();
            var totals = (from item in db.OrderItems
                          join order in db.Orders on item.OrderId equals order.OrderId
                          where order.StatusCode == "COMPLETED" && ids.Contains(item.BookId)
                          group item by item.BookId into items
                          select new { BookId = items.Key, Total = items.Sum(x => (long)x.Quantity) })
                         .ToDictionary(x => x.BookId, x => x.Total);
            foreach (var book in books)
                book.SoldCount = totals.TryGetValue(book.BookId, out var total) ? total : 0;
        }

        public ActionResult Index()
        {
            using (var db = new DatabaseDataContext())
            {
                var banner = CustomerCache.Banner(db, null, true);
                return View(banner);
            }
        }

        internal static bool HasUsableCover(Book book)
        {
            var url = (book.CoverUrl ?? "").Trim().Replace('\\', '/');
            if (url.StartsWith("~/", StringComparison.Ordinal)) url = url.Substring(1);
            if (!url.StartsWith("/Content/", StringComparison.OrdinalIgnoreCase) || url.Contains("..") || url.IndexOfAny(new[] { '?', '#' }) >= 0)
                return false;
            try
            {
                var path = HostingEnvironment.MapPath("~" + url);
                if (path == null || !System.IO.File.Exists(path)) return false;
                using (var image = Image.FromFile(path)) return image.Width > 0 && image.Height > 0;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is System.Runtime.InteropServices.ExternalException || ex is OutOfMemoryException || ex is HttpException)
            {
                return false;
            }
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
    internal static class CustomerCache
    {
        private static readonly object Sync = new object();

        private static T GetOrCreate<T>(string key, int seconds, Func<T> factory) where T : class
        {
            var value = HttpRuntime.Cache[key] as T;
            if (value != null) return value;
            lock (Sync)
            {
                value = HttpRuntime.Cache[key] as T;
                if (value != null) return value;
                value = factory();
                HttpRuntime.Cache.Insert(key, value, null, DateTime.UtcNow.AddSeconds(seconds), System.Web.Caching.Cache.NoSlidingExpiration);
                return value;
            }
        }

        internal static List<Category> Categories(DatabaseDataContext db, bool refresh = false)
        {
            const string key = "customer:categories:v1";
            if (refresh) HttpRuntime.Cache.Remove(key);
            return GetOrCreate(key, 60, () => db.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name)
                .Select(c => new { c.CategoryId, c.Name, c.CategoryCode }).ToList()
                .Select(c => new Category { CategoryId = c.CategoryId, Name = c.Name, CategoryCode = c.CategoryCode }).ToList());
        }

        internal static List<long> TopBookIds(DatabaseDataContext db)
        {
            return GetOrCreate("customer:top-books:v1", 120, () =>
                (from item in db.OrderItems
                 join order in db.Orders on item.OrderId equals order.OrderId
                 join book in db.Books on item.BookId equals book.BookId
                 where order.StatusCode == "COMPLETED" && book.Active && !book.IsDeleted
                 group item by item.BookId into items
                 let total = items.Sum(x => (long)x.Quantity)
                 orderby total descending, items.Key descending
                 select items.Key).Take(10).ToList());
        }

        internal static List<Book> Banner(DatabaseDataContext db, int? categoryId, bool onlyInStock)
        {
            var date = DateTime.UtcNow.AddHours(7).Date;
            var key = "customer:banner:v1:" + (categoryId.HasValue ? categoryId.Value.ToString() : "all") + ":" + onlyInStock + ":" + date.ToString("yyyyMMdd");
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var selected = GetOrCreate(key, 60, () =>
                {
                    var source = db.Books.Where(b => b.Active && !b.IsDeleted && b.CoverUrl != null && b.CoverUrl != "");
                    if (categoryId.HasValue) source = source.Where(b => b.CategoryId == categoryId.Value);
                    if (onlyInStock) source = source.Where(b => b.Stock > 0);
                    var candidates = source.OrderBy(b => b.BookId).Select(b => new { b.BookId, b.Title, b.CoverUrl, b.Stock }).ToList()
                        .Select(b => new Book { BookId = b.BookId, Title = b.Title, CoverUrl = b.CoverUrl, Stock = b.Stock })
                        .Where(HomeController.HasUsableCover).ToList();
                    var day = (long)(date - new DateTime(2000, 1, 1)).TotalDays;
                    var available = Rotate(candidates.Where(b => b.Stock > 0).ToList(), day, 3);
                    if (available.Count < 3 && !onlyInStock)
                        available.AddRange(Rotate(candidates.Where(b => b.Stock <= 0).ToList(), day, 3 - available.Count));
                    return available;
                });
                if (selected.Count == 0) return new List<Book>();
                var ids = selected.Select(b => b.BookId).ToList();
                var fresh = db.Books.Where(b => ids.Contains(b.BookId) && b.Active && !b.IsDeleted).ToList();
                if (fresh.Count == selected.Count && selected.All(s => fresh.Any(b => b.BookId == s.BookId && b.Stock == s.Stock && b.CoverUrl == s.CoverUrl && HomeController.HasUsableCover(b))))
                    return fresh.OrderBy(b => ids.IndexOf(b.BookId)).ToList();
                HttpRuntime.Cache.Remove(key);
            }
            return new List<Book>();
        }

        private static List<Book> Rotate(List<Book> books, long day, int max)
        {
            if (books.Count == 0 || max <= 0) return new List<Book>();
            var start = (int)(((day * 3) % books.Count + books.Count) % books.Count);
            return Enumerable.Range(0, Math.Min(max, books.Count)).Select(i => books[(start + i) % books.Count]).ToList();
        }
    }

}
