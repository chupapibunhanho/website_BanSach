using System;
using System.Web.Mvc;
using webstie_BanSach.Business;

namespace webstie_BanSach.Controllers
{
    public class AuthorMgmtController : Controller
    {
        private readonly AuthorMgmt_ctrl _biz = new AuthorMgmt_ctrl();

        public ActionResult AuthorList() { return View(); }
        public ActionResult AddAuthor() { return View(); }
        public ActionResult UpdateAuthor(int? id) { ViewBag.AuthorId = id; return View(); }

        [HttpPost]
        public JsonResult GetAuthorList(string keyword, string status, int page = 1)
        {
            var result = _biz.GetPagedList(keyword, status, page);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAuthorInfo(int id)
        {
            var result = _biz.GetInfo(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Insert(string txt_AuthorName)
        {
            var result = _biz.Create(txt_AuthorName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Update(int txt_AuthorId, string txt_AuthorName, bool chk_IsDeleted = false)
        {
            var result = _biz.Update(txt_AuthorId, txt_AuthorName, chk_IsDeleted);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            var result = _biz.Delete(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
