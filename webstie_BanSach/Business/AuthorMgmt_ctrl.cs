using System;
using System.Collections.Generic;
using System.Linq;
using webstie_BanSach.Models;

namespace webstie_BanSach.Business
{
    public class AuthorMgmt_ctrl
    {
        private const int PageSize = 10;

        public class AuthorListItemDto
        {
            public int AuthorId { get; set; }
            public string AuthorName { get; set; }
            public bool IsDeleted { get; set; }
            public string UpdatedAtUtc { get; set; }
        }

        public class AuthorPagedResult
        {
            public List<AuthorListItemDto> Items { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int TotalRecords { get; set; }
        }

        public class AuthorDetailDto
        {
            public int AuthorId { get; set; }
            public string AuthorName { get; set; }
            public bool IsDeleted { get; set; }
        }

        public FunctResult<AuthorPagedResult> GetPagedList(string keyword, string status, int page, string sortBy = "UpdatedAtUtc", string sortDir = "desc")
        {
            var result = new FunctResult<AuthorPagedResult>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    IQueryable<Author> query = db.Authors;
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        string kw = keyword.Trim().ToLower();
                        query = query.Where(a => a.AuthorName.ToLower().Contains(kw));
                    }
                    switch ((status ?? "active").Trim().ToLower())
                    {
                        case "deleted": query = query.Where(a => a.IsDeleted); break;
                        case "all": break;
                        default: query = query.Where(a => !a.IsDeleted); break;
                    }

                    int totalRecords = query.Count();
                    int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
                    if (totalPages < 1) totalPages = 1;
                    if (page < 1) page = 1;
                    if (page > totalPages) page = totalPages;

                    bool asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
                    IOrderedQueryable<Author> ordered;
                    switch ((sortBy ?? "").Trim().ToLower())
                    {
                        case "authorid": ordered = asc ? query.OrderBy(a => a.AuthorId) : query.OrderByDescending(a => a.AuthorId); break;
                        case "authorname": ordered = asc ? query.OrderBy(a => a.AuthorName).ThenBy(a => a.AuthorId) : query.OrderByDescending(a => a.AuthorName).ThenByDescending(a => a.AuthorId); break;
                        case "isdeleted": ordered = asc ? query.OrderBy(a => a.IsDeleted).ThenByDescending(a => a.AuthorId) : query.OrderByDescending(a => a.IsDeleted).ThenByDescending(a => a.AuthorId); break;
                        default: ordered = asc ? query.OrderBy(a => a.UpdatedAtUtc).ThenBy(a => a.AuthorId) : query.OrderByDescending(a => a.UpdatedAtUtc).ThenByDescending(a => a.AuthorId); break;
                    }

                    var rawItems = ordered
                        .Skip((page - 1) * PageSize).Take(PageSize)
                        .Select(a => new { a.AuthorId, a.AuthorName, a.IsDeleted, a.UpdatedAtUtc }).ToList();

                    var items = rawItems.Select(a => new AuthorListItemDto
                    {
                        AuthorId = a.AuthorId,
                        AuthorName = a.AuthorName,
                        IsDeleted = a.IsDeleted,
                        UpdatedAtUtc = a.UpdatedAtUtc.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = totalRecords > 0 ? "Lấy danh sách tác giả thành công" : "Không có dữ liệu";
                    result.Data = new AuthorPagedResult { Items = items, CurrentPage = page, TotalPages = totalPages, TotalRecords = totalRecords };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi lấy danh sách tác giả: " + ex.Message;
                result.Data = new AuthorPagedResult { Items = new List<AuthorListItemDto>(), CurrentPage = 1, TotalPages = 1, TotalRecords = 0 };
            }
            return result;
        }

        public FunctResult<AuthorDetailDto> GetInfo(int authorId)
        {
            var result = new FunctResult<AuthorDetailDto>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var a = db.Authors.FirstOrDefault(x => x.AuthorId == authorId);
                    if (a == null) { result.ErrCode = (int)EnumErrCode.NotExist; result.ErrDesc = "Tác giả không tồn tại"; return result; }
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Lấy thông tin tác giả thành công";
                    result.Data = new AuthorDetailDto { AuthorId = a.AuthorId, AuthorName = a.AuthorName, IsDeleted = a.IsDeleted };
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi lấy thông tin tác giả: " + ex.Message; }
            return result;
        }

        public FunctResult<AuthorDetailDto> Create(string authorName)
        {
            var result = new FunctResult<AuthorDetailDto>();
            try
            {
                authorName = (authorName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(authorName)) { result.ErrCode = (int)EnumErrCode.MissingInput; result.ErrDesc = "Vui lòng nhập tên tác giả"; return result; }
                using (var db = new DatabaseDataContext())
                {
                    bool exists = db.Authors.Any(x => !x.IsDeleted && x.AuthorName.ToLower() == authorName.ToLower());
                    if (exists) { result.ErrCode = (int)EnumErrCode.Existent; result.ErrDesc = "Tên tác giả đã tồn tại"; return result; }
                    var entity = new Author { AuthorName = authorName, IsDeleted = false, UpdatedAtUtc = DateTime.UtcNow, DeletedAtUtc = null };
                    db.Authors.InsertOnSubmit(entity);
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Thêm tác giả thành công";
                    result.Data = new AuthorDetailDto { AuthorId = entity.AuthorId, AuthorName = entity.AuthorName, IsDeleted = entity.IsDeleted };
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi thêm tác giả: " + ex.Message; }
            return result;
        }

        public FunctResult<AuthorDetailDto> Update(int authorId, string authorName, bool isDeleted)
        {
            var result = new FunctResult<AuthorDetailDto>();
            try
            {
                authorName = (authorName ?? "").Trim();
                if (authorId <= 0 || string.IsNullOrWhiteSpace(authorName)) { result.ErrCode = (int)EnumErrCode.MissingInput; result.ErrDesc = "Vui lòng nhập đầy đủ thông tin tác giả"; return result; }
                using (var db = new DatabaseDataContext())
                {
                    var a = db.Authors.FirstOrDefault(x => x.AuthorId == authorId);
                    if (a == null) { result.ErrCode = (int)EnumErrCode.NotExist; result.ErrDesc = "Tác giả không tồn tại"; return result; }
                    bool dup = db.Authors.Any(x => x.AuthorId != authorId && !x.IsDeleted && x.AuthorName.ToLower() == authorName.ToLower());
                    if (dup) { result.ErrCode = (int)EnumErrCode.Existent; result.ErrDesc = "Tên tác giả đã tồn tại"; return result; }
                    a.AuthorName = authorName;
                    a.IsDeleted = isDeleted;
                    a.UpdatedAtUtc = DateTime.UtcNow;
                    a.DeletedAtUtc = isDeleted ? (DateTime?)DateTime.UtcNow : null;
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Cập nhật tác giả thành công";
                    result.Data = new AuthorDetailDto { AuthorId = a.AuthorId, AuthorName = a.AuthorName, IsDeleted = a.IsDeleted };
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi cập nhật tác giả: " + ex.Message; }
            return result;
        }

        public FunctResult<bool> Delete(int authorId)
        {
            var result = new FunctResult<bool>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var a = db.Authors.FirstOrDefault(x => x.AuthorId == authorId);
                    if (a == null) { result.ErrCode = (int)EnumErrCode.NotExist; result.ErrDesc = "Tác giả không tồn tại"; result.Data = false; return result; }
                    if (a.IsDeleted) { result.ErrCode = (int)EnumErrCode.Success; result.ErrDesc = "Tác giả đã được xóa trước đó"; result.Data = true; return result; }
                    a.IsDeleted = true; a.UpdatedAtUtc = DateTime.UtcNow; a.DeletedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success; result.ErrDesc = "Xóa tác giả thành công"; result.Data = true;
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi xóa tác giả: " + ex.Message; result.Data = false; }
            return result;
        }
    }
}
