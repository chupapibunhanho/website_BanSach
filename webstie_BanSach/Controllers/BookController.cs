using System;
using System.Linq;
using System.Web.Mvc;
using webstie_BanSach.Models;
using webstie_BanSach.ViewModels;

namespace webstie_BanSach.Controllers
{
    public class BookController : Controller
    {
        [HttpGet]
        public ActionResult Index(int? categoryId, int page = 1)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(400);
            const int pageSize = 20;
            using (var db = new DatabaseDataContext())
            {
                var categories = db.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name)
                    .Select(c => new CategoryCountViewModel
                    {
                        CategoryId = c.CategoryId,
                        Name = c.Name,
                        Code = c.CategoryCode,
                        Count = db.Books.Count(b => b.CategoryId == c.CategoryId && b.Active && !b.IsDeleted)
                    }).ToList();
                var category = categoryId.HasValue ? categories.FirstOrDefault(c => c.CategoryId == categoryId.Value) : null;
                if (categoryId.HasValue && category == null) return HttpNotFound();

                var allBooks = db.Books.Where(b => b.Active && !b.IsDeleted);
                var allCount = allBooks.Count();
                var query = categoryId.HasValue ? allBooks.Where(b => b.CategoryId == categoryId.Value) : allBooks;
                var count = categoryId.HasValue ? category.Count : allCount;
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                var books = query.OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.BookId)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var candidates = query.Where(b => b.CoverUrl != null && b.CoverUrl != "")
                    .OrderBy(b => b.BookId).Select(b => new { b.BookId, b.Title, b.CoverUrl, b.Stock }).ToList()
                    .Select(b => new Book { BookId = b.BookId, Title = b.Title, CoverUrl = b.CoverUrl, Stock = b.Stock })
                    .Where(HomeController.HasUsableCover).ToList();
                var day = (long)(DateTime.UtcNow.AddHours(7).Date - new DateTime(2000, 1, 1)).TotalDays;
                var available = candidates.Where(b => b.Stock > 0).ToList();
                var others = candidates.Where(b => b.Stock <= 0).ToList();
                var banner = RotateDaily(available, day, 3);
                if (banner.Count < 3) banner.AddRange(RotateDaily(others, day, 3 - banner.Count));
                var ids = books.Select(b => b.BookId).ToList();
                if (ids.Count > 0)
                {
                    var authors = (from ba in db.BookAuthors join author in db.Authors on ba.AuthorId equals author.AuthorId
                                   where ids.Contains(ba.BookId) && !author.IsDeleted
                                   select new { ba.BookId, author.AuthorName }).ToList()
                                  .GroupBy(a => a.BookId).ToDictionary(g => g.Key, g => string.Join(", ", g.Select(a => a.AuthorName).Distinct()));
                    var ratings = db.BookReviews.Where(r => !r.IsDeleted && r.Rating > 0 && ids.Contains(r.BookId))
                        .GroupBy(r => r.BookId).Select(g => new { Id = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
                        .ToDictionary(r => r.Id);
                    var sales = (from item in db.OrderItems join order in db.Orders on item.OrderId equals order.OrderId
                                 where order.StatusCode == "COMPLETED" && ids.Contains(item.BookId)
                                 group item by item.BookId into g
                                 select new { Id = g.Key, Total = g.Sum(i => (long)i.Quantity) }).ToDictionary(s => s.Id);
                    var top = (from item in db.OrderItems join order in db.Orders on item.OrderId equals order.OrderId
                               join book in db.Books on item.BookId equals book.BookId
                               where order.StatusCode == "COMPLETED" && book.Active && !book.IsDeleted
                               group item by item.BookId into g
                               let total = g.Sum(i => (long)i.Quantity)
                               orderby total descending, g.Key descending select g.Key).Take(3).ToList();
                    foreach (var book in books)
                    {
                        if (ratings.TryGetValue(book.BookId, out var rating)) { book.AverageRating = rating.Average; book.RatingCount = rating.Count; }
                        book.SoldCount = sales.TryGetValue(book.BookId, out var sold) ? sold.Total : 0;
                        var rank = top.IndexOf(book.BookId);
                        if (rank >= 0) book.SalesRank = rank + 1;
                        book.AuthorNames = authors.TryGetValue(book.BookId, out var names) ? names : null;
                    }
                }
                var model = new BookListViewModel { Books = books, Categories = categories, SelectedCategoryId = categoryId,
                    SelectedCategoryName = category == null ? null : category.Name, PageNumber = page,
                    TotalPages = totalPages, TotalCount = count, AllBooksCount = allCount, SelectedCategoryCode = category == null ? null : category.Code, BannerBooks = banner };
                if (Request.IsAjaxRequest()) return PartialView("_BookListContent", model);
                return View(model);
            }
        }

        private static System.Collections.Generic.List<Book> RotateDaily(System.Collections.Generic.List<Book> books, long day, int max)
        {
            if (books.Count == 0 || max <= 0) return new System.Collections.Generic.List<Book>();
            var start = (int)(((day * 3) % books.Count + books.Count) % books.Count);
            return Enumerable.Range(0, Math.Min(max, books.Count)).Select(i => books[(start + i) % books.Count]).ToList();
        }

        [HttpGet]
        public ActionResult QuickView(long id)
        {
            using (var db = new DatabaseDataContext())
            {
                var book = db.Books.FirstOrDefault(b => b.BookId == id && b.Active && !b.IsDeleted);
                if (book == null) return HttpNotFound();
                var names = (from ba in db.BookAuthors join a in db.Authors on ba.AuthorId equals a.AuthorId
                             where ba.BookId == id && !a.IsDeleted select a.AuthorName).ToList();
                var ratings = db.BookReviews.Where(r => r.BookId == id && !r.IsDeleted && r.Rating > 0);
                book.RatingCount = ratings.Count();
                book.AverageRating = book.RatingCount > 0 ? ratings.Average(r => (double)r.Rating) : 0;
                book.SoldCount = (from item in db.OrderItems join order in db.Orders on item.OrderId equals order.OrderId
                                  where item.BookId == id && order.StatusCode == "COMPLETED"
                                  select (long?)item.Quantity).Sum() ?? 0;
                var category = db.Categories.Where(c => c.CategoryId == book.CategoryId && !c.IsDeleted).Select(c => c.Name).FirstOrDefault();
                var publisher = db.Publishers.Where(p => p.PublisherId == book.PublisherId && !p.IsDeleted).Select(p => p.PublisherName).FirstOrDefault();
                return PartialView("_QuickView", new QuickViewModel { Book = book, Authors = string.Join(", ", names), Category = category, Publisher = publisher });
            }
        }
    }
}
