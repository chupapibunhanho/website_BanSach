using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using webstie_BanSach.Business;

namespace webstie_BanSach.Controllers
{
    [webstie_BanSach.Security.AdminAccess]
    public class BookMgmtController : Controller
    {
        private readonly BookMgmt_ctrl _bookBusiness = new BookMgmt_ctrl();

        public ActionResult BookList()
        {
            return View();
        }

        public ActionResult AddBook()
        {
            return View();
        }

        public ActionResult UpdateBook(long? id)
        {
            ViewBag.BookId = id;
            return View();
        }

        [HttpPost]
        public JsonResult GetBookList(string keyword, int? categoryId, int? publisherId, string status, int page = 1, string sortBy = "UpdatedAtUtc", string sortDir = "desc")
        {
            var result = _bookBusiness.GetPagedList(keyword, categoryId, publisherId, status, page, sortBy, sortDir);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDropdownData()
        {
            var result = _bookBusiness.GetDropdownData();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetBookInfo(long id)
        {
            var result = _bookBusiness.GetInfo(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Insert(string txt_ISBN13, string txt_Title, decimal? txt_Price, int? txt_CategoryId, int? txt_PublisherId, int? txt_Stock, string txt_CoverUrl, string txt_Description, bool chk_Active = true, string txt_AuthorIds = null)
        {
            var authorIds = ParseIds(txt_AuthorIds);
            var result = _bookBusiness.Create(txt_ISBN13, txt_Title, txt_Price, txt_CategoryId, txt_PublisherId, txt_Stock, txt_CoverUrl, txt_Description, chk_Active, authorIds);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Update(long txt_BookId, string txt_ISBN13, string txt_Title, decimal? txt_Price, int? txt_CategoryId, int? txt_PublisherId, int? txt_Stock, string txt_CoverUrl, string txt_Description, bool chk_Active = true, bool chk_IsDeleted = false, string txt_AuthorIds = null)
        {
            var authorIds = ParseIds(txt_AuthorIds);
            var result = _bookBusiness.Update(txt_BookId, txt_ISBN13, txt_Title, txt_Price, txt_CategoryId, txt_PublisherId, txt_Stock, txt_CoverUrl, txt_Description, chk_Active, chk_IsDeleted, authorIds);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Delete(long id)
        {
            var result = _bookBusiness.Delete(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private List<int> ParseIds(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<int>();
            return raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => { int v; return int.TryParse(s.Trim(), out v) ? v : 0; })
                .Where(v => v > 0).Distinct().ToList();
        }
    }
}
