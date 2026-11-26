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
                var top = (from item in db.OrderItems
                           join order in db.Orders on item.OrderId equals order.OrderId
                           join book in db.Books on item.BookId equals book.BookId
                           where order.StatusCode == "COMPLETED" && book.Active && !book.IsDeleted
                           group item by item.BookId into items
                           let total = items.Sum(x => (long)x.Quantity)
                           orderby total descending, items.Key descending
                           select new { BookId = items.Key, Total = total }).Take(10).ToList();
                var ids = top.Select(x => x.BookId).ToList();
                var books = db.Books.Where(b => ids.Contains(b.BookId) && !b.IsDeleted && b.Active).ToList();
                var rankedBooks = top.Select((entry, index) => new { entry.BookId, entry.Total, Rank = index + 1 })
                    .ToDictionary(entry => entry.BookId);
                foreach (var book in books)
                {
                    var ranking = rankedBooks[book.BookId];
                    book.SoldCount = ranking.Total;
                    book.SalesRank = ranking.Rank;
                }
                ApplyRatings(db, books);
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
                return PartialView("Partials/_CategoriesList", db.Categories
                    .Where(c => !c.IsDeleted).OrderBy(c => c.Name).Take(12).ToList());
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
                var candidates = db.Books.Where(b => b.Active && !b.IsDeleted && b.Stock > 0 && b.CoverUrl != null && b.CoverUrl != "")
                    .OrderBy(b => b.BookId).ToList();
                var booksWithCovers = candidates.Where(HasUsableCover).ToList();
                return View(SelectDailyBooks(booksWithCovers, DateTime.UtcNow.AddHours(7).Date));
            }
        }

        private static List<Book> SelectDailyBooks(List<Book> books, DateTime vietnamDate)
        {
            if (books.Count == 0) return new List<Book>();
            var day = (long)(vietnamDate.Date - new DateTime(2000, 1, 1)).TotalDays;
            var start = (int)(((day * 3) % books.Count + books.Count) % books.Count);
            return Enumerable.Range(0, Math.Min(3, books.Count))
                .Select(i => books[(start + i) % books.Count]).ToList();
        }

        private static bool HasUsableCover(Book book)
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
}
