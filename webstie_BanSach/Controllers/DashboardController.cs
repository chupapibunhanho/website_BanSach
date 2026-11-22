using System.Web.Mvc;
using webstie_BanSach.Business;

namespace webstie_BanSach.Controllers
{
    public class DashboardController : Controller
    {
        private readonly Dashboard_ctrl _biz = new Dashboard_ctrl();

        public ActionResult Dashboard()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetDashboard(int days = 30)
        {
            var result = _biz.GetDashboard(days);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
