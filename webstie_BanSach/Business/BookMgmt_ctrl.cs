using System;
using System.Collections.Generic;
using System.Linq;
using webstie_BanSach.Models;

namespace webstie_BanSach.Business
{
    public class BookMgmt_ctrl
    {
        private const int PageSize = 10;

        public class BookListItemDto
        {
            public long BookId { get; set; }
            public string ISBN13 { get; set; }
            public string CoverUrl { get; set; }
            public string Title { get; set; }
            public string CategoryName { get; set; }
            public string PublisherName { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public bool Active { get; set; }
            public bool IsDeleted { get; set; }
            public string UpdatedAtUtc { get; set; }
        }

        public class BookPagedResult
        {
            public List<BookListItemDto> Items { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int TotalRecords { get; set; }
        }

        public class OptionItemDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class BookDropdownDataDto
        {
            public List<OptionItemDto> Categories { get; set; }
            public List<OptionItemDto> Publishers { get; set; }
            public List<OptionItemDto> Authors { get; set; }
        }

        public class BookDetailDto
        {
            public long BookId { get; set; }
            public string ISBN13 { get; set; }
            public string Title { get; set; }
            public int? CategoryId { get; set; }
            public int? PublisherId { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public string CoverUrl { get; set; }
            public string Description { get; set; }
            public bool Active { get; set; }
            public bool IsDeleted { get; set; }
            public List<int> AuthorIds { get; set; }
        }

        private class BookQueryRow
        {
            public Book B { get; set; }
            public string CategoryName { get; set; }
            public string PublisherName { get; set; }
        }

        public FunctResult<BookPagedResult> GetPagedList(string keyword, int? categoryId, int? publisherId, string status, int page, string sortBy = "UpdatedAtUtc", string sortDir = "desc")
        {
            var result = new FunctResult<BookPagedResult>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var query = from b in db.Books
                                join c in db.Categories on b.CategoryId equals c.CategoryId into cj
                                from c in cj.DefaultIfEmpty()
                                join p in db.Publishers on b.PublisherId equals p.PublisherId into pj
                                from p in pj.DefaultIfEmpty()
                                select new BookQueryRow { B = b, CategoryName = c != null ? c.Name : "", PublisherName = p != null ? p.PublisherName : "" };

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        string kw = keyword.Trim().ToLower();
                        query = query.Where(x =>
                            x.B.Title.ToLower().Contains(kw) ||
                            x.B.ISBN13.ToLower().Contains(kw) ||
                            (x.CategoryName ?? "").ToLower().Contains(kw) ||
                            (x.PublisherName ?? "").ToLower().Contains(kw));
                    }

                    if (categoryId.HasValue && categoryId.Value > 0)
                        query = query.Where(x => x.B.CategoryId == categoryId.Value);

                    if (publisherId.HasValue && publisherId.Value > 0)
                        query = query.Where(x => x.B.PublisherId == publisherId.Value);

                    switch ((status ?? "active").Trim().ToLower())
                    {
                        case "inactive":
                            query = query.Where(x => !x.B.IsDeleted && !x.B.Active);
                            break;
                        case "deleted":
                            query = query.Where(x => x.B.IsDeleted);
                            break;
                        case "all":
                            break;
                        default:
                            query = query.Where(x => !x.B.IsDeleted && x.B.Active);
                            break;
                    }

                    int totalRecords = query.Count();
                    int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
                    if (totalPages < 1) totalPages = 1;
                    if (page < 1) page = 1;
                    if (page > totalPages) page = totalPages;

                    bool asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
                    var s = (sortBy ?? "").Trim().ToLower();
                    IOrderedQueryable<BookQueryRow> sortedQ;
                    switch (s)
                    {
                        case "bookid": sortedQ = asc ? query.OrderBy(x => x.B.BookId) : query.OrderByDescending(x => x.B.BookId); break;
                        case "isbn13": sortedQ = asc ? query.OrderBy(x => x.B.ISBN13).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.B.ISBN13).ThenByDescending(x => x.B.BookId); break;
                        case "title": sortedQ = asc ? query.OrderBy(x => x.B.Title).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.B.Title).ThenByDescending(x => x.B.BookId); break;
                        case "categoryname": sortedQ = asc ? query.OrderBy(x => x.CategoryName).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.CategoryName).ThenByDescending(x => x.B.BookId); break;
                        case "publishername": sortedQ = asc ? query.OrderBy(x => x.PublisherName).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.PublisherName).ThenByDescending(x => x.B.BookId); break;
                        case "price": sortedQ = asc ? query.OrderBy(x => x.B.Price).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.B.Price).ThenByDescending(x => x.B.BookId); break;
                        case "stock": sortedQ = asc ? query.OrderBy(x => x.B.Stock).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.B.Stock).ThenByDescending(x => x.B.BookId); break;
                        case "status": sortedQ = asc ? query.OrderBy(x => x.B.IsDeleted).ThenBy(x => x.B.Active).ThenByDescending(x => x.B.BookId) : query.OrderByDescending(x => x.B.IsDeleted).ThenByDescending(x => x.B.Active).ThenByDescending(x => x.B.BookId); break;
                        default: sortedQ = asc ? query.OrderBy(x => x.B.UpdatedAtUtc).ThenBy(x => x.B.BookId) : query.OrderByDescending(x => x.B.UpdatedAtUtc).ThenByDescending(x => x.B.BookId); break;
                    }

                    var rawItems = sortedQ
                        .Skip((page - 1) * PageSize)
                        .Take(PageSize)
                        .Select(x => new
                        {
                            BookId = x.B.BookId,
                            ISBN13 = x.B.ISBN13,
                            CoverUrl = x.B.CoverUrl,
                            Title = x.B.Title,
                            CategoryName = x.CategoryName,
                            PublisherName = x.PublisherName,
                            Price = x.B.Price,
                            Stock = x.B.Stock,
                            Active = x.B.Active,
                            IsDeleted = x.B.IsDeleted,
                            UpdatedAtUtc = x.B.UpdatedAtUtc
                        })
                        .ToList();

                    var items = rawItems.Select(x => new BookListItemDto
                    {
                        BookId = x.BookId,
                        ISBN13 = x.ISBN13,
                        CoverUrl = x.CoverUrl,
                        Title = x.Title,
                        CategoryName = x.CategoryName ?? "",
                        PublisherName = x.PublisherName ?? "",
                        Price = x.Price,
                        Stock = x.Stock,
                        Active = x.Active,
                        IsDeleted = x.IsDeleted,
                        UpdatedAtUtc = x.UpdatedAtUtc.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = totalRecords > 0 ? "Lấy danh sách sách thành công" : "Không có dữ liệu";
                    result.Data = new BookPagedResult
                    {
                        Items = items,
                        CurrentPage = page,
                        TotalPages = totalPages,
                        TotalRecords = totalRecords
                    };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi lấy danh sách sách: " + ex.Message;
                result.Data = new BookPagedResult { Items = new List<BookListItemDto>(), CurrentPage = 1, TotalPages = 1, TotalRecords = 0 };
            }
            return result;
        }

        public FunctResult<BookDropdownDataDto> GetDropdownData()
        {
            var result = new FunctResult<BookDropdownDataDto>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    result.Data = new BookDropdownDataDto
                    {
                        Categories = db.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name)
                            .Select(c => new OptionItemDto { Id = c.CategoryId, Name = c.Name }).ToList(),
                        Publishers = db.Publishers.Where(p => !p.IsDeleted).OrderBy(p => p.PublisherName)
                            .Select(p => new OptionItemDto { Id = p.PublisherId, Name = p.PublisherName }).ToList(),
                        Authors = db.Authors.Where(a => !a.IsDeleted).OrderBy(a => a.AuthorName)
                            .Select(a => new OptionItemDto { Id = a.AuthorId, Name = a.AuthorName }).ToList()
                    };
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Lấy dữ liệu dropdown thành công";
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi lấy dữ liệu dropdown: " + ex.Message;
            }
            return result;
        }

        public FunctResult<BookDetailDto> GetInfo(long bookId)
        {
            var result = new FunctResult<BookDetailDto>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var book = db.Books.FirstOrDefault(b => b.BookId == bookId);
                    if (book == null)
                    {
                        result.ErrCode = (int)EnumErrCode.NotExist;
                        result.ErrDesc = "Sách không tồn tại";
                        return result;
                    }

                    var authorIds = db.BookAuthors.Where(ba => ba.BookId == bookId).Select(ba => ba.AuthorId).ToList();
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Lấy thông tin sách thành công";
                    result.Data = new BookDetailDto
                    {
                        BookId = book.BookId,
                        ISBN13 = book.ISBN13,
                        Title = book.Title,
                        CategoryId = book.CategoryId,
                        PublisherId = book.PublisherId,
                        Price = book.Price,
                        Stock = book.Stock,
                        CoverUrl = book.CoverUrl,
                        Description = book.Description,
                        Active = book.Active,
                        IsDeleted = book.IsDeleted,
                        AuthorIds = authorIds
                    };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi lấy thông tin sách: " + ex.Message;
            }
            return result;
        }

        public FunctResult<object> Create(string isbn13, string title, decimal? price, int? categoryId, int? publisherId, int? stock, string coverUrl, string description, bool active, List<int> authorIds)
        {
            var result = new FunctResult<object>();
            try
            {
                isbn13 = (isbn13 ?? "").Trim();
                title = (title ?? "").Trim();
                coverUrl = (coverUrl ?? "").Trim();
                description = (description ?? "").Trim();

                if (string.IsNullOrWhiteSpace(isbn13) || string.IsNullOrWhiteSpace(title) || !price.HasValue)
                {
                    result.ErrCode = (int)EnumErrCode.MissingInput;
                    result.ErrDesc = "Vui lòng nhập ISBN, tên sách và giá";
                    return result;
                }
                if (isbn13.Length != 13)
                {
                    result.ErrCode = (int)EnumErrCode.InvalidInput;
                    result.ErrDesc = "ISBN13 phải gồm 13 ký tự";
                    return result;
                }

                using (var db = new DatabaseDataContext())
                {
                    if (db.Books.Any(b => b.ISBN13 == isbn13 && !b.IsDeleted))
                    {
                        result.ErrCode = (int)EnumErrCode.Existent;
                        result.ErrDesc = "ISBN13 đã tồn tại";
                        return result;
                    }

                    var now = DateTime.UtcNow;
                    var book = new Book
                    {
                        ISBN13 = isbn13,
                        Title = title,
                        CategoryId = categoryId,
                        PublisherId = publisherId,
                        Price = price.Value,
                        Stock = stock ?? 0,
                        CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl,
                        Description = string.IsNullOrWhiteSpace(description) ? null : description,
                        Active = active,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now,
                        IsDeleted = false,
                        DeletedAtUtc = null
                    };
                    db.Books.InsertOnSubmit(book);
                    db.SubmitChanges();

                    if (authorIds != null)
                    {
                        var validAuthorIds = authorIds.Where(id => id > 0).Distinct().ToList();
                        foreach (var aid in validAuthorIds)
                        {
                            if (db.Authors.Any(a => a.AuthorId == aid && !a.IsDeleted))
                                db.BookAuthors.InsertOnSubmit(new BookAuthor { BookId = book.BookId, AuthorId = aid });
                        }
                        db.SubmitChanges();
                    }

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Thêm sách thành công";
                    result.Data = new { book.BookId };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi thêm sách: " + ex.Message;
            }
            return result;
        }

        public FunctResult<object> Update(long bookId, string isbn13, string title, decimal? price, int? categoryId, int? publisherId, int? stock, string coverUrl, string description, bool active, bool isDeleted, List<int> authorIds)
        {
            var result = new FunctResult<object>();
            try
            {
                isbn13 = (isbn13 ?? "").Trim();
                title = (title ?? "").Trim();
                coverUrl = (coverUrl ?? "").Trim();
                description = (description ?? "").Trim();

                if (bookId <= 0 || string.IsNullOrWhiteSpace(isbn13) || string.IsNullOrWhiteSpace(title) || !price.HasValue)
                {
                    result.ErrCode = (int)EnumErrCode.MissingInput;
                    result.ErrDesc = "Vui lòng nhập đầy đủ thông tin sách";
                    return result;
                }

                using (var db = new DatabaseDataContext())
                {
                    var book = db.Books.FirstOrDefault(b => b.BookId == bookId);
                    if (book == null)
                    {
                        result.ErrCode = (int)EnumErrCode.NotExist;
                        result.ErrDesc = "Sách không tồn tại";
                        return result;
                    }

                    bool dupIsbn = db.Books.Any(b => b.BookId != bookId && b.ISBN13 == isbn13 && !b.IsDeleted);
                    if (dupIsbn)
                    {
                        result.ErrCode = (int)EnumErrCode.Existent;
                        result.ErrDesc = "ISBN13 đã tồn tại";
                        return result;
                    }

                    book.ISBN13 = isbn13;
                    book.Title = title;
                    book.CategoryId = categoryId;
                    book.PublisherId = publisherId;
                    book.Price = price.Value;
                    book.Stock = stock ?? 0;
                    book.CoverUrl = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl;
                    book.Description = string.IsNullOrWhiteSpace(description) ? null : description;
                    book.Active = active;
                    book.IsDeleted = isDeleted;
                    book.UpdatedAtUtc = DateTime.UtcNow;
                    book.DeletedAtUtc = isDeleted ? (DateTime?)DateTime.UtcNow : null;

                    var oldAuthors = db.BookAuthors.Where(ba => ba.BookId == bookId).ToList();
                    if (oldAuthors.Any()) db.BookAuthors.DeleteAllOnSubmit(oldAuthors);
                    if (authorIds != null)
                    {
                        var validAuthorIds = authorIds.Where(id => id > 0).Distinct().ToList();
                        foreach (var aid in validAuthorIds)
                        {
                            if (db.Authors.Any(a => a.AuthorId == aid && !a.IsDeleted))
                                db.BookAuthors.InsertOnSubmit(new BookAuthor { BookId = bookId, AuthorId = aid });
                        }
                    }

                    db.SubmitChanges();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Cập nhật sách thành công";
                    result.Data = new { book.BookId };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi cập nhật sách: " + ex.Message;
            }
            return result;
        }

        public FunctResult<bool> Delete(long bookId)
        {
            var result = new FunctResult<bool>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var book = db.Books.FirstOrDefault(b => b.BookId == bookId);
                    if (book == null)
                    {
                        result.ErrCode = (int)EnumErrCode.NotExist;
                        result.ErrDesc = "Sách không tồn tại";
                        result.Data = false;
                        return result;
                    }
                    if (book.IsDeleted)
                    {
                        result.ErrCode = (int)EnumErrCode.Success;
                        result.ErrDesc = "Sách đã được xóa trước đó";
                        result.Data = true;
                        return result;
                    }
                    book.IsDeleted = true;
                    book.Active = false;
                    book.UpdatedAtUtc = DateTime.UtcNow;
                    book.DeletedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Xóa sách thành công";
                    result.Data = true;
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi xóa sách: " + ex.Message;
                result.Data = false;
            }
            return result;
        }
    }
}
