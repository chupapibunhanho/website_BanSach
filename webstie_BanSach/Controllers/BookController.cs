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
                var categoryInfo = CustomerCache.Categories(db);
                if (categoryId.HasValue && !categoryInfo.Any(c => c.CategoryId == categoryId.Value))
                    categoryInfo = CustomerCache.Categories(db, true);
                var counts = db.Books.Where(b => b.Active && !b.IsDeleted).GroupBy(b => b.CategoryId)
                    .Select(g => new { Id = g.Key, Count = g.Count() }).ToList();
                var categories = categoryInfo.Select(c => new CategoryCountViewModel { CategoryId = c.CategoryId, Name = c.Name,
                    Code = c.CategoryCode, Count = counts.Where(x => x.Id == c.CategoryId).Select(x => x.Count).FirstOrDefault() }).ToList();
                var category = categoryId.HasValue ? categories.FirstOrDefault(c => c.CategoryId == categoryId.Value) : null;
                if (categoryId.HasValue && category == null) return HttpNotFound();

                var allBooks = db.Books.Where(b => b.Active && !b.IsDeleted);
                var allCount = counts.Sum(c => c.Count);
                var query = categoryId.HasValue ? allBooks.Where(b => b.CategoryId == categoryId.Value) : allBooks;
                var count = categoryId.HasValue ? category.Count : allCount;
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                var books = query.OrderByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.BookId)
                    .Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var banner = CustomerCache.Banner(db, categoryId, false);
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
                    var top = CustomerCache.TopBookIds(db).Take(3).ToList();
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
