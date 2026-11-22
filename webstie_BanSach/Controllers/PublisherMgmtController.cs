using System;
using System.Web.Mvc;
using webstie_BanSach.Business;

namespace webstie_BanSach.Controllers
{
    public class PublisherMgmtController : Controller
    {
        private readonly PublisherMgmt_ctrl _biz = new PublisherMgmt_ctrl();

        public ActionResult PublisherList() { return View(); }
        public ActionResult AddPublisher() { return View(); }
        public ActionResult UpdatePublisher(int? id) { ViewBag.PublisherId = id; return View(); }

        [HttpPost]
        public JsonResult GetPublisherList(string keyword, string status, int page = 1, string sortBy = "UpdatedAtUtc", string sortDir = "desc")
        {
            var result = _biz.GetPagedList(keyword, status, page, sortBy, sortDir);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetPublisherInfo(int id)
        {
            var result = _biz.GetInfo(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Insert(string txt_PublisherName)
        {
            var result = _biz.Create(txt_PublisherName);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Update(int txt_PublisherId, string txt_PublisherName, bool chk_IsDeleted = false)
        {
            var result = _biz.Update(txt_PublisherId, txt_PublisherName, chk_IsDeleted);
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
