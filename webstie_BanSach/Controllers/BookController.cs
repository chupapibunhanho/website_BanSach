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
                var ids = books.Select(b => b.BookId).ToList();
                if (ids.Count > 0)
                {
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
                    }
                }
                return View(new BookListViewModel { Books = books, Categories = categories, SelectedCategoryId = categoryId,
                    SelectedCategoryName = category == null ? null : category.Name, PageNumber = page,
                    TotalPages = totalPages, TotalCount = count, AllBooksCount = allCount });
            }
        }
    }
}
