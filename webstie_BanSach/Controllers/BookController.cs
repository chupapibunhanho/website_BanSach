using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using webstie_BanSach.Models;
using webstie_BanSach.ViewModels;

namespace webstie_BanSach.Controllers
{
    public class BookController : Controller
    {
        [HttpGet]
        public ActionResult Index(int? categoryId, int page = 1, string q = null, int? authorId = null, int? publisherId = null,
            decimal? minPrice = null, decimal? maxPrice = null, int? minRating = null, string stock = null, string sort = null)
        {
            if (!ModelState.IsValid) return new HttpStatusCodeResult(400);
            q = NormalizeSearch(q);
            stock = (stock ?? "").Trim().ToLowerInvariant();
            sort = (sort ?? "").Trim().ToLowerInvariant();
            var orders = new Dictionary<string, string> {
                { "", "b.CreatedAtUtc DESC, b.BookId DESC" }, { "newest", "b.CreatedAtUtc DESC, b.BookId DESC" },
                { "bestselling", "COALESCE((SELECT SUM(CONVERT(bigint, oi.Quantity)) FROM dbo.OrderItems oi JOIN dbo.Orders o ON o.OrderId=oi.OrderId WHERE oi.BookId=b.BookId AND o.StatusCode='COMPLETED'),0) DESC, b.BookId DESC" },
                { "price_asc", "b.Price ASC, b.BookId DESC" }, { "price_desc", "b.Price DESC, b.BookId DESC" },
                { "rating", "COALESCE((SELECT AVG(CONVERT(float,r.Rating)) FROM dbo.BookReviews r WHERE r.BookId=b.BookId AND r.IsDeleted=0 AND r.Rating>0),0) DESC, b.BookId DESC" },
                { "title_asc", "b.Title COLLATE Latin1_General_100_CI_AI ASC, b.BookId DESC" }, { "title_desc", "b.Title COLLATE Latin1_General_100_CI_AI DESC, b.BookId DESC" }
            };
            if (q.Length > 200 || !orders.ContainsKey(sort) || (stock != "" && stock != "in" && stock != "out") ||
                minPrice < 0 || maxPrice < 0 || (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice) ||
                (minRating.HasValue && (minRating < 1 || minRating > 5))) return new HttpStatusCodeResult(400);
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
                var args = new List<object>();
                Func<object, string> parameter = value => { args.Add(value); return "{" + (args.Count - 1) + "}"; };
                var where = " WHERE b.Active=1 AND b.IsDeleted=0";
                if (categoryId.HasValue) where += " AND b.CategoryId=" + parameter(categoryId.Value);
                if (authorId.HasValue) where += " AND EXISTS (SELECT 1 FROM dbo.BookAuthors ba JOIN dbo.Authors a ON a.AuthorId=ba.AuthorId WHERE ba.BookId=b.BookId AND a.IsDeleted=0 AND a.AuthorId=" + parameter(authorId.Value) + ")";
                if (publisherId.HasValue) where += " AND b.PublisherId=" + parameter(publisherId.Value);
                if (minPrice.HasValue) where += " AND b.Price >= " + parameter(minPrice.Value);
                if (maxPrice.HasValue) where += " AND b.Price <= " + parameter(maxPrice.Value);
                if (minRating.HasValue) where += " AND COALESCE((SELECT AVG(CONVERT(float,r.Rating)) FROM dbo.BookReviews r WHERE r.BookId=b.BookId AND r.IsDeleted=0 AND r.Rating>0),0) >= " + parameter(minRating.Value);
                if (stock == "in") where += " AND b.Stock > 0";
                if (stock == "out") where += " AND b.Stock <= 0";
                if (q.Length > 0)
                {
                    var pattern = parameter("%" + EscapeLike(q) + "%");
                    where += " AND (REPLACE(REPLACE(b.Title,N'  ',N' '),N'  ',N' ') COLLATE Latin1_General_100_CI_AI LIKE " + pattern + " ESCAPE N'~'" +
                             " OR b.ISBN13 COLLATE Latin1_General_100_CI_AI LIKE " + pattern + " ESCAPE N'~'" +
                             " OR EXISTS (SELECT 1 FROM dbo.BookAuthors ba JOIN dbo.Authors a ON a.AuthorId=ba.AuthorId WHERE ba.BookId=b.BookId AND a.IsDeleted=0 AND REPLACE(REPLACE(a.AuthorName,N'  ',N' '),N'  ',N' ') COLLATE Latin1_General_100_CI_AI LIKE " + pattern + " ESCAPE N'~'))";
                }
                var count = db.ExecuteQuery<int>("SELECT COUNT(*) FROM dbo.Books b" + where, args.ToArray()).Single();
                var totalPages = (int)Math.Ceiling(count / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                var offset = parameter((page - 1) * pageSize);
                var limit = parameter(pageSize);
                var pageIds = db.ExecuteQuery<long>("SELECT b.BookId FROM dbo.Books b" + where + " ORDER BY " + orders[sort] + " OFFSET " + offset + " ROWS FETCH NEXT " + limit + " ROWS ONLY", args.ToArray()).ToList();
                var books = allBooks.Where(b => pageIds.Contains(b.BookId)).ToList().OrderBy(b => pageIds.IndexOf(b.BookId)).ToList();
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
                    TotalPages = totalPages, TotalCount = count, AllBooksCount = allCount, SelectedCategoryCode = category == null ? null : category.Code, BannerBooks = banner,
                    SearchTerm = q, AuthorId = authorId, PublisherId = publisherId, MinPrice = minPrice, MaxPrice = maxPrice, MinRating = minRating, Stock = stock, Sort = sort,
                    Authors = CustomerCache.Authors(db), Publishers = CustomerCache.Publishers(db) };
                if (Request.IsAjaxRequest()) return PartialView("_BookListContent", model);
                return View(model);
            }
        }

        private static string NormalizeSearch(string text) { return Regex.Replace((text ?? "").Trim(), @"\s+", " "); }
        private static string EscapeLike(string text) { return text.Replace("~", "~~").Replace("%", "~%").Replace("_", "~_").Replace("[", "~["); }

        [HttpGet]
        public ActionResult Search(string q)
        {
            q = NormalizeSearch(q);
            if (q.Length < 2 || q.Length > 200) return Json(new object[0], JsonRequestBehavior.AllowGet);
            using (var db = new DatabaseDataContext())
            {
                var pattern = "%" + EscapeLike(q) + "%";
                var ids = db.ExecuteQuery<long>("SELECT TOP (5) b.BookId FROM dbo.Books b WHERE b.Active=1 AND b.IsDeleted=0 AND (REPLACE(REPLACE(b.Title,N'  ',N' '),N'  ',N' ') COLLATE Latin1_General_100_CI_AI LIKE {0} ESCAPE N'~' OR b.ISBN13 COLLATE Latin1_General_100_CI_AI LIKE {0} ESCAPE N'~' OR EXISTS (SELECT 1 FROM dbo.BookAuthors ba JOIN dbo.Authors a ON a.AuthorId=ba.AuthorId WHERE ba.BookId=b.BookId AND a.IsDeleted=0 AND REPLACE(REPLACE(a.AuthorName,N'  ',N' '),N'  ',N' ') COLLATE Latin1_General_100_CI_AI LIKE {0} ESCAPE N'~')) ORDER BY b.CreatedAtUtc DESC,b.BookId DESC", pattern).ToList();
                var books = db.Books.Where(b => ids.Contains(b.BookId) && b.Active && !b.IsDeleted).Select(b => new { b.BookId, b.Title, b.CoverUrl, b.Price }).ToList();
                var authors = (from ba in db.BookAuthors join a in db.Authors on ba.AuthorId equals a.AuthorId
                               where ids.Contains(ba.BookId) && !a.IsDeleted select new { ba.BookId, a.AuthorName }).ToList()
                              .GroupBy(x => x.BookId).ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.AuthorName).Distinct()));
                var result = books.OrderBy(b => ids.IndexOf(b.BookId)).Select(b => new { id = b.BookId, title = b.Title,
                    cover = string.IsNullOrWhiteSpace(b.CoverUrl) ? Url.Content("~/Content/Images/default-book.jpg") : Url.Content(b.CoverUrl),
                    price = b.Price, authors = authors.TryGetValue(b.BookId, out var name) ? name : "" }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult Details(long id)
        {
            using (var db = new DatabaseDataContext())
            {
                var book = db.Books.FirstOrDefault(b => b.BookId == id && b.Active && !b.IsDeleted);
                if (book == null) return HttpNotFound();
                var authorIds = db.BookAuthors.Where(a => a.BookId == id).Select(a => a.AuthorId).ToList();
                var authors = db.Authors.Where(a => authorIds.Contains(a.AuthorId) && !a.IsDeleted).Select(a => a.AuthorName).ToList();
                var category = db.Categories.Where(c => c.CategoryId == book.CategoryId && !c.IsDeleted).Select(c => c.Name).FirstOrDefault();
                var publisher = db.Publishers.Where(p => p.PublisherId == book.PublisherId && !p.IsDeleted).Select(p => p.PublisherName).FirstOrDefault();
                var related = db.Books.Where(b => b.BookId != id && b.Active && !b.IsDeleted &&
                    ((book.CategoryId.HasValue && b.CategoryId == book.CategoryId) || db.BookAuthors.Any(a => a.BookId == b.BookId && authorIds.Contains(a.AuthorId))))
                    .OrderByDescending(b => db.BookAuthors.Any(a => a.BookId == b.BookId && authorIds.Contains(a.AuthorId)))
                    .ThenByDescending(b => b.Stock > 0).ThenByDescending(b => b.CreatedAtUtc).ThenByDescending(b => b.BookId).Take(8).ToList();
                var togetherIds = (from item in db.OrderItems
                                   join order in db.Orders on item.OrderId equals order.OrderId
                                   join candidate in db.Books on item.BookId equals candidate.BookId
                                   where order.StatusCode == "COMPLETED" && item.BookId != id && candidate.Active && !candidate.IsDeleted &&
                                     db.OrderItems.Any(original => original.OrderId == item.OrderId && original.BookId == id)
                                   group item by item.BookId into g
                                   let orders = g.Select(x => x.OrderId).Distinct().Count()
                                   where orders >= 2
                                   orderby orders descending, g.Key descending
                                   select g.Key).Take(4).ToList();
                var together = db.Books.Where(b => togetherIds.Contains(b.BookId) && b.Active && !b.IsDeleted).ToList()
                    .OrderBy(b => togetherIds.IndexOf(b.BookId)).ToList();
                var ratings = db.BookReviews.Where(r => r.BookId == id && !r.IsDeleted && r.Rating >= 1 && r.Rating <= 5)
                    .GroupBy(r => r.Rating).Select(g => new { Rating = g.Key, Count = g.Count() }).ToList();
                var counts = new int[6]; foreach (var rating in ratings) counts[rating.Rating] = rating.Count;
                PopulateBookStats(db, new[] { book }.Concat(related).Concat(together).ToList());
                return View(new BookDetailViewModel { Book = book, Authors = string.Join(", ", authors), Category = category, Publisher = publisher,
                    RatingCounts = counts, RelatedBooks = related, BoughtTogether = together, ReviewPage = GetReviews(db, id, null, "newest", 1, counts.Sum()) });
            }
        }

        [HttpGet]
        public ActionResult Reviews(long id, int? rating = null, string sort = "newest", int page = 1)
        {
            if (!ModelState.IsValid || (rating.HasValue && (rating < 1 || rating > 5)) || (sort != "newest" && sort != "highest" && sort != "lowest")) return new HttpStatusCodeResult(400);
            using (var db = new DatabaseDataContext())
            {
                if (!db.Books.Any(b => b.BookId == id && b.Active && !b.IsDeleted)) return HttpNotFound();
                return PartialView("_Reviews", GetReviews(db, id, rating, sort, page));
            }
        }

        private static ReviewPageViewModel GetReviews(DatabaseDataContext db, long id, int? rating, string sort, int page, int? knownCount = null)
        {
            const int size = 5;
            var query = db.BookReviews.Where(r => r.BookId == id && !r.IsDeleted && r.Rating >= 1 && r.Rating <= 5);
            if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);
            var count = knownCount ?? query.Count(); var pages = (int)Math.Ceiling(count / (double)size);
            page = Math.Max(1, Math.Min(page, Math.Max(1, pages)));
            var ordered = sort == "highest" ? query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.ReviewId)
                : sort == "lowest" ? query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.ReviewId)
                : query.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.ReviewId);
            var reviews = ordered.Skip((page - 1) * size).Take(size).Select(r => new ReviewItemViewModel {
                ReviewerName = r.User.FullName, Rating = r.Rating, Title = r.Title, Comment = r.Comment, CreatedAtUtc = r.CreatedAtUtc }).ToList();
            return new ReviewPageViewModel { BookId = id, Reviews = reviews, Rating = rating, Sort = sort, Page = page, TotalPages = pages, TotalCount = count };
        }

        [HttpGet]
        public ActionResult Recent(string ids)
        {
            var ordered = (ids ?? "").Split(',').Take(10).Select(s => long.TryParse(s, out var value) ? value : 0)
                .Where(x => x > 0).Distinct().ToList();
            using (var db = new DatabaseDataContext())
            {
                var books = db.Books.Where(b => ordered.Contains(b.BookId) && b.Active && !b.IsDeleted).ToList()
                    .OrderBy(b => ordered.IndexOf(b.BookId)).ToList();
                PopulateBookStats(db, books);
                Response.Cache.SetNoStore();
                return PartialView("_BookSuggestions", books);
            }
        }

        private static void PopulateBookStats(DatabaseDataContext db, List<Book> books)
        {
            var ids = books.Select(b => b.BookId).Distinct().ToList(); if (ids.Count == 0) return;
            var ratings = db.BookReviews.Where(r => !r.IsDeleted && r.Rating >= 1 && r.Rating <= 5 && ids.Contains(r.BookId))
                .GroupBy(r => r.BookId).Select(g => new { Id = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() }).ToDictionary(x => x.Id);
            var sales = (from i in db.OrderItems join o in db.Orders on i.OrderId equals o.OrderId where o.StatusCode == "COMPLETED" && ids.Contains(i.BookId)
                         group i by i.BookId into g select new { Id = g.Key, Total = g.Sum(x => (long)x.Quantity) }).ToDictionary(x => x.Id);
            var top = CustomerCache.TopBookIds(db);
            foreach (var b in books) { if (ratings.TryGetValue(b.BookId, out var rating)) { b.AverageRating = rating.Average; b.RatingCount = rating.Count; }
                b.SoldCount = sales.TryGetValue(b.BookId, out var sold) ? sold.Total : 0; var rank = top.IndexOf(b.BookId); if (rank >= 0 && rank < 3) b.SalesRank = rank + 1; }
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
