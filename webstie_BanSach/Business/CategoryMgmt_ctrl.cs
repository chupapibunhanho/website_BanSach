using System;
using System.Collections.Generic;
using System.Linq;
using webstie_BanSach.Models;

namespace webstie_BanSach.Business
{
    public class CategoryMgmt_ctrl
    {
        private const int PageSize = 10;

        public class CategoryListItemDto
        {
            public int CategoryId { get; set; }
            public string CategoryCode { get; set; }
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
            public string UpdatedAtUtc { get; set; }
        }

        public class CategoryPagedResult
        {
            public List<CategoryListItemDto> Items { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int TotalRecords { get; set; }
        }

        public class CategoryDetailDto
        {
            public int CategoryId { get; set; }
            public string CategoryCode { get; set; }
            public string Name { get; set; }
            public bool IsDeleted { get; set; }
        }

        public FunctResult<CategoryPagedResult> GetPagedList(string keyword, string status, int page, string sortBy = "UpdatedAtUtc", string sortDir = "desc")
        {
            var result = new FunctResult<CategoryPagedResult>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    IQueryable<Category> query = db.Categories;

                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        string kw = keyword.Trim().ToLower();
                        query = query.Where(c =>
                            c.CategoryCode.ToLower().Contains(kw) ||
                            c.Name.ToLower().Contains(kw));
                    }

                    switch ((status ?? "active").Trim().ToLower())
                    {
                        case "deleted":
                            query = query.Where(c => c.IsDeleted);
                            break;
                        case "all":
                            break;
                        default:
                            query = query.Where(c => !c.IsDeleted);
                            break;
                    }

                    int totalRecords = query.Count();
                    int totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
                    if (totalPages < 1) totalPages = 1;
                    if (page < 1) page = 1;
                    if (page > totalPages) page = totalPages;

                    bool asc = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
                    IOrderedQueryable<Category> ordered;
                    switch ((sortBy ?? "").Trim().ToLower())
                    {
                        case "categoryid":
                            ordered = asc ? query.OrderBy(c => c.CategoryId) : query.OrderByDescending(c => c.CategoryId);
                            break;
                        case "categorycode":
                            ordered = asc ? query.OrderBy(c => c.CategoryCode).ThenBy(c => c.CategoryId) : query.OrderByDescending(c => c.CategoryCode).ThenByDescending(c => c.CategoryId);
                            break;
                        case "name":
                            ordered = asc ? query.OrderBy(c => c.Name).ThenBy(c => c.CategoryId) : query.OrderByDescending(c => c.Name).ThenByDescending(c => c.CategoryId);
                            break;
                        case "isdeleted":
                            ordered = asc ? query.OrderBy(c => c.IsDeleted).ThenByDescending(c => c.CategoryId) : query.OrderByDescending(c => c.IsDeleted).ThenByDescending(c => c.CategoryId);
                            break;
                        default:
                            ordered = asc ? query.OrderBy(c => c.UpdatedAtUtc).ThenBy(c => c.CategoryId) : query.OrderByDescending(c => c.UpdatedAtUtc).ThenByDescending(c => c.CategoryId);
                            break;
                    }

                    var rawItems = ordered
                        .Skip((page - 1) * PageSize)
                        .Take(PageSize)
                        .Select(c => new
                        {
                            c.CategoryId,
                            c.CategoryCode,
                            c.Name,
                            c.IsDeleted,
                            c.UpdatedAtUtc
                        })
                        .ToList();

                    var items = rawItems.Select(c => new CategoryListItemDto
                    {
                        CategoryId = c.CategoryId,
                        CategoryCode = c.CategoryCode,
                        Name = c.Name,
                        IsDeleted = c.IsDeleted,
                        UpdatedAtUtc = c.UpdatedAtUtc.ToString("dd/MM/yyyy HH:mm")
                    }).ToList();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = totalRecords > 0 ? "Lấy danh sách danh mục thành công" : "Không có dữ liệu";
                    result.Data = new CategoryPagedResult
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
                result.ErrDesc = "Có lỗi xảy ra khi lấy danh sách danh mục: " + ex.Message;
                result.Data = new CategoryPagedResult
                {
                    Items = new List<CategoryListItemDto>(),
                    CurrentPage = 1,
                    TotalPages = 1,
                    TotalRecords = 0
                };
            }

            return result;
        }

        public FunctResult<CategoryDetailDto> GetInfo(int categoryId)
        {
            var result = new FunctResult<CategoryDetailDto>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var category = db.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
                    if (category == null)
                    {
                        result.ErrCode = (int)EnumErrCode.NotExist;
                        result.ErrDesc = "Danh mục không tồn tại";
                        return result;
                    }

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Lấy thông tin danh mục thành công";
                    result.Data = new CategoryDetailDto
                    {
                        CategoryId = category.CategoryId,
                        CategoryCode = category.CategoryCode,
                        Name = category.Name,
                        IsDeleted = category.IsDeleted
                    };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi lấy thông tin danh mục: " + ex.Message;
            }

            return result;
        }

        public FunctResult<CategoryDetailDto> Create(string categoryCode, string name)
        {
            var result = new FunctResult<CategoryDetailDto>();
            try
            {
                categoryCode = (categoryCode ?? string.Empty).Trim();
                name = (name ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(categoryCode) || string.IsNullOrWhiteSpace(name))
                {
                    result.ErrCode = (int)EnumErrCode.MissingInput;
                    result.ErrDesc = "Vui lòng nhập đầy đủ mã và tên danh mục";
                    return result;
                }

                using (var db = new DatabaseDataContext())
                {
                    bool exists = db.Categories.Any(c => !c.IsDeleted && c.CategoryCode.ToLower() == categoryCode.ToLower());
                    if (exists)
                    {
                        result.ErrCode = (int)EnumErrCode.Existent;
                        result.ErrDesc = "Mã danh mục đã tồn tại";
                        return result;
                    }

                    var category = new Category
                    {
                        CategoryCode = categoryCode,
                        Name = name,
                        IsDeleted = false,
                        UpdatedAtUtc = DateTime.UtcNow,
                        DeletedAtUtc = null
                    };

                    db.Categories.InsertOnSubmit(category);
                    db.SubmitChanges();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Thêm danh mục thành công";
                    result.Data = new CategoryDetailDto
                    {
                        CategoryId = category.CategoryId,
                        CategoryCode = category.CategoryCode,
                        Name = category.Name,
                        IsDeleted = category.IsDeleted
                    };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi thêm danh mục: " + ex.Message;
            }

            return result;
        }

        public FunctResult<CategoryDetailDto> Update(int categoryId, string categoryCode, string name, bool isDeleted)
        {
            var result = new FunctResult<CategoryDetailDto>();
            try
            {
                categoryCode = (categoryCode ?? string.Empty).Trim();
                name = (name ?? string.Empty).Trim();

                if (categoryId <= 0 || string.IsNullOrWhiteSpace(categoryCode) || string.IsNullOrWhiteSpace(name))
                {
                    result.ErrCode = (int)EnumErrCode.MissingInput;
                    result.ErrDesc = "Vui lòng nhập đầy đủ thông tin danh mục";
                    return result;
                }

                using (var db = new DatabaseDataContext())
                {
                    var category = db.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
                    if (category == null)
                    {
                        result.ErrCode = (int)EnumErrCode.NotExist;
                        result.ErrDesc = "Danh mục không tồn tại";
                        return result;
                    }

                    bool duplicateCode = db.Categories.Any(c =>
                        c.CategoryId != categoryId &&
                        !c.IsDeleted &&
                        c.CategoryCode.ToLower() == categoryCode.ToLower());
                    if (duplicateCode)
                    {
                        result.ErrCode = (int)EnumErrCode.Existent;
                        result.ErrDesc = "Mã danh mục đã tồn tại";
                        return result;
                    }

                    category.CategoryCode = categoryCode;
                    category.Name = name;
                    category.IsDeleted = isDeleted;
                    category.UpdatedAtUtc = DateTime.UtcNow;
                    category.DeletedAtUtc = isDeleted ? (DateTime?)DateTime.UtcNow : null;

                    db.SubmitChanges();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Cập nhật danh mục thành công";
                    result.Data = new CategoryDetailDto
                    {
                        CategoryId = category.CategoryId,
                        CategoryCode = category.CategoryCode,
                        Name = category.Name,
                        IsDeleted = category.IsDeleted
                    };
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi cập nhật danh mục: " + ex.Message;
            }

            return result;
        }

        public FunctResult<bool> Delete(int categoryId)
        {
            var result = new FunctResult<bool>();
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var category = db.Categories.FirstOrDefault(c => c.CategoryId == categoryId);
                    if (category == null)
                    {
                        result.ErrCode = (int)EnumErrCode.NotExist;
                        result.ErrDesc = "Danh mục không tồn tại";
                        result.Data = false;
                        return result;
                    }

                    if (category.IsDeleted)
                    {
                        result.ErrCode = (int)EnumErrCode.Success;
                        result.ErrDesc = "Danh mục đã được xóa trước đó";
                        result.Data = true;
                        return result;
                    }

                    category.IsDeleted = true;
                    category.UpdatedAtUtc = DateTime.UtcNow;
                    category.DeletedAtUtc = DateTime.UtcNow;
                    db.SubmitChanges();

                    result.ErrCode = (int)EnumErrCode.Success;
                    result.ErrDesc = "Xóa danh mục thành công";
                    result.Data = true;
                }
            }
            catch (Exception ex)
            {
                result.ErrCode = (int)EnumErrCode.Error;
                result.ErrDesc = "Có lỗi xảy ra khi xóa danh mục: " + ex.Message;
                result.Data = false;
            }

            return result;
        }
    }
}
