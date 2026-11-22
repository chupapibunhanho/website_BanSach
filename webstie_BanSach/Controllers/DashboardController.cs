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
        public JsonResult GetDashboard(string range = "30", string days = null)
        {
            var value = !string.IsNullOrWhiteSpace(range) ? range : days;
            var result = _biz.GetDashboard(value);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}
