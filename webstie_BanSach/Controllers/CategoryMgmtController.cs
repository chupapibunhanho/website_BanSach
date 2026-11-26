using System;
using System.Web.Mvc;
using webstie_BanSach.Business;

namespace webstie_BanSach.Controllers
{
    [webstie_BanSach.Security.AdminAccess]
    public class CategoryMgmtController : Controller
    {
        private readonly CategoryMgmt_ctrl _categoryBusiness = new CategoryMgmt_ctrl();

        public ActionResult CategoryList()
        {
            return View();
        }

        public ActionResult AddCategory()
        {
            return View();
        }

        public ActionResult UpdateCategory(int? id)
        {
            ViewBag.CategoryId = id;
            return View();
        }

        [HttpPost]
        public JsonResult GetCategoryList(string keyword, string status, int page = 1, string sortBy = "UpdatedAtUtc", string sortDir = "desc")
        {
            var result = _categoryBusiness.GetPagedList(keyword, status, page, sortBy, sortDir);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCategoryInfo(int id)
        {
            var result = _categoryBusiness.GetInfo(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Insert(string txt_CategoryCode, string txt_Name)
        {
            var result = _categoryBusiness.Create(txt_CategoryCode, txt_Name);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Update(int txt_CategoryId, string txt_CategoryCode, string txt_Name, bool chk_IsDeleted = false)
        {
            var result = _categoryBusiness.Update(txt_CategoryId, txt_CategoryCode, txt_Name, chk_IsDeleted);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            var result = _categoryBusiness.Delete(id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
