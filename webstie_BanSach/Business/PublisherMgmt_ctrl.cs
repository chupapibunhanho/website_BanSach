using System;
using System.Collections.Generic;
using System.Linq;
using webstie_BanSach.Models;

namespace webstie_BanSach.Business
{
    public class PublisherMgmt_ctrl
    {
        private const int PageSize = 10;

        public class PublisherListItemDto
        {
            public int PublisherId { get; set; }
            public string PublisherName { get; set; }
            public bool IsDeleted { get; set; }
            public string UpdatedAtUtc { get; set; }
        }

        public class PublisherPagedResult
        {
            public List<PublisherListItemDto> Items { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int TotalRecords { get; set; }
        }

        public class PublisherDetailDto
        {
            public int PublisherId { get; set; }
            public string PublisherName { get; set; }
            public bool IsDeleted { get; set; }
        }

        public FunctResult<PublisherPagedResult> GetPagedList(string keyword, string status, int page)
        {
            var result = new FunctResult<PublisherPagedResult>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    IQueryable<Publisher> query = db.Publishers;
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        string kw = keyword.Trim().ToLower();
                        query = query.Where(p => p.PublisherName.ToLower().Contains(kw));
                    }
                    switch ((status ?? "active").Trim().ToLower())
                    {
                        case "deleted": query = query.Where(p => p.IsDeleted); break;
                        case "all": break;
                        default: query = query.Where(p => !p.IsDeleted); break;
                    }

                    int totalRecords = query.Count();
                    int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
                    if (totalPages < 1) totalPages = 1;
                    if (page < 1) page = 1;
                    if (page > totalPages) page = totalPages;

                    var rawItems = query.OrderByDescending(p => p.UpdatedAtUtc).ThenByDescending(p => p.PublisherId)
                        .Skip((page - 1) * PageSize).Take(PageSize)
                        .Select(p => new { p.PublisherId, p.PublisherName, p.IsDeleted, p.UpdatedAtUtc }).ToList();

                    var items = rawItems.Select(p => new PublisherListItemDto
                    {
                        PublisherId = p.PublisherId,
                        PublisherName = p.PublisherName,
                        IsDeleted = p.IsDeleted,
                        UpdatedAtUtc = p.UpdatedAtUtc.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = totalRecords > 0 ? "Lấy danh sách nhà xuất bản thành công" : "Không có dữ liệu";
                    result.Data = new PublisherPagedResult { Items = items, CurrentPage = page, TotalPages = totalPages, TotalRecords = totalRecords };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi lấy danh sách nhà xuất bản: " + ex.Message;
                result.Data = new PublisherPagedResult { Items = new List<PublisherListItemDto>(), CurrentPage = 1, TotalPages = 1, TotalRecords = 0 };
            }
            return result;
        }

        public FunctResult<PublisherDetailDto> GetInfo(int publisherId)
        {
            var result = new FunctResult<PublisherDetailDto>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var p = db.Publishers.FirstOrDefault(x => x.PublisherId == publisherId);
                    if (p == null) { result.ErrCode = (int)EnumErrCode.NotExist; result.ErrDesc = "Nhà xuất bản không tồn tại"; return result; }
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Lấy thông tin nhà xuất bản thành công";
                    result.Data = new PublisherDetailDto { PublisherId = p.PublisherId, PublisherName = p.PublisherName, IsDeleted = p.IsDeleted };
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi lấy thông tin nhà xuất bản: " + ex.Message; }
            return result;
        }

        public FunctResult<PublisherDetailDto> Create(string publisherName)
        {
            var result = new FunctResult<PublisherDetailDto>();
            try
            {
                publisherName = (publisherName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(publisherName)) { result.ErrCode = (int)EnumErrCode.MissingInput; result.ErrDesc = "Vui lòng nhập tên nhà xuất bản"; return result; }
                using (var db = new DatabaseDataContext())
                {
                    bool exists = db.Publishers.Any(x => !x.IsDeleted && x.PublisherName.ToLower() == publisherName.ToLower());
                    if (exists) { result.ErrCode = (int)EnumErrCode.Existent; result.ErrDesc = "Tên nhà xuất bản đã tồn tại"; return result; }
                    var entity = new Publisher { PublisherName = publisherName, IsDeleted = false, UpdatedAtUtc = DateTime.UtcNow, DeletedAtUtc = null };
                    db.Publishers.InsertOnSubmit(entity);
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Thêm nhà xuất bản thành công";
                    result.Data = new PublisherDetailDto { PublisherId = entity.PublisherId, PublisherName = entity.PublisherName, IsDeleted = entity.IsDeleted };
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi thêm nhà xuất bản: " + ex.Message; }
            return result;
        }

        public FunctResult<PublisherDetailDto> Update(int publisherId, string publisherName, bool isDeleted)
        {
            var result = new FunctResult<PublisherDetailDto>();
            try
            {
                publisherName = (publisherName ?? "").Trim();
                if (publisherId <= 0 || string.IsNullOrWhiteSpace(publisherName)) { result.ErrCode = (int)EnumErrCode.MissingInput; result.ErrDesc = "Vui lòng nhập đầy đủ thông tin nhà xuất bản"; return result; }
                using (var db = new DatabaseDataContext())
                {
                    var p = db.Publishers.FirstOrDefault(x => x.PublisherId == publisherId);
                    if (p == null) { result.ErrCode = (int)EnumErrCode.NotExist; result.ErrDesc = "Nhà xuất bản không tồn tại"; return result; }
                    bool dup = db.Publishers.Any(x => x.PublisherId != publisherId && !x.IsDeleted && x.PublisherName.ToLower() == publisherName.ToLower());
                    if (dup) { result.ErrCode = (int)EnumErrCode.Existent; result.ErrDesc = "Tên nhà xuất bản đã tồn tại"; return result; }
                    p.PublisherName = publisherName;
                    p.IsDeleted = isDeleted;
                    p.UpdatedAtUtc = DateTime.UtcNow;
                    p.DeletedAtUtc = isDeleted ? (DateTime?)DateTime.UtcNow : null;
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Cập nhật nhà xuất bản thành công";
                    result.Data = new PublisherDetailDto { PublisherId = p.PublisherId, PublisherName = p.PublisherName, IsDeleted = p.IsDeleted };
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi cập nhật nhà xuất bản: " + ex.Message; }
            return result;
        }

        public FunctResult<bool> Delete(int publisherId)
        {
            var result = new FunctResult<bool>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var p = db.Publishers.FirstOrDefault(x => x.PublisherId == publisherId);
                    if (p == null) { result.ErrCode = (int)EnumErrCode.NotExist; result.ErrDesc = "Nhà xuất bản không tồn tại"; result.Data = false; return result; }
                    if (p.IsDeleted) { result.ErrCode = (int)EnumErrCode.Success; result.ErrDesc = "Nhà xuất bản đã được xóa trước đó"; result.Data = true; return result; }
                    p.IsDeleted = true; p.UpdatedAtUtc = DateTime.UtcNow; p.DeletedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();
                    result.ErrCode = (int)EnumErrCode.Success; result.ErrDesc = "Xóa nhà xuất bản thành công"; result.Data = true;
                }
            }
            catch (Exception ex) { result.ErrCode = (int)EnumErrCode.Error; result.ErrDesc = "Có lỗi xảy ra khi xóa nhà xuất bản: " + ex.Message; result.Data = false; }
            return result;
        }
    }
}
