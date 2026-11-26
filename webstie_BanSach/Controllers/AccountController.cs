using System;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using webstie_BanSach.Models;
using webstie_BanSach.Security;
using webstie_BanSach.ViewModels;

namespace webstie_BanSach.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            NoCache();
            if (User.Identity.IsAuthenticated) return RedirectAfterLogin(User.IsInRole("ADMIN"), returnUrl);
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            NoCache();
            if (model == null) model = new LoginViewModel();
            model.Email = (model.Email ?? "").Trim();
            if (User.Identity.IsAuthenticated) return RedirectAfterLogin(User.IsInRole("ADMIN"), model.ReturnUrl);
            if (string.IsNullOrEmpty(model.Email)) ModelState.AddModelError("Email", "Vui lòng nhập email.");
            else if (model.Email.Length > 255 || !ValidEmail(model.Email)) ModelState.AddModelError("Email", "Vui lòng nhập email hợp lệ.");
            if (string.IsNullOrEmpty(model.Password)) ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu.");
            else if (model.Password.Length > 255) ModelState.AddModelError("Password", "Mật khẩu không hợp lệ.");
            if (!ModelState.IsValid) return View(model);
            var ip = Request.UserHostAddress ?? "";
            if (AuthSessions.IsLimited(model.Email, ip)) { ModelState.AddModelError("", "Tạm thời không thể đăng nhập. Vui lòng thử lại sau vài phút."); return View(model); }
            try
            {
                using (var db = new DatabaseDataContext())
                {
                    var user = (from u in db.Users join role in db.Roles on u.RoleId equals role.RoleId
                                where u.Email == model.Email select new { u.UserId, u.Password, u.IsActive, role.RoleName }).FirstOrDefault();
                    var passwordMatches = AuthSessions.FixedTimeEquals(user == null ? "" : user.Password, model.Password);
                    var roleName = user == null ? "" : user.RoleName;
                    var allowed = string.Equals(roleName, "ADMIN", StringComparison.OrdinalIgnoreCase) || string.Equals(roleName, "CUSTOMER", StringComparison.OrdinalIgnoreCase);
                    if (user == null || !passwordMatches || !user.IsActive || !allowed)
                    {
                        AuthSessions.Failed(model.Email, ip); ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác."); return View(model);
                    }
                    AuthSessions.Succeeded(model.Email);
                    if (Session != null) { Session.Clear(); Session.Abandon(); var manager = new SessionIDManager(); manager.RemoveSessionID(System.Web.HttpContext.Current); var id = manager.CreateSessionID(System.Web.HttpContext.Current); manager.SaveSessionID(System.Web.HttpContext.Current, id, out _, out _); }
                    AuthSessions.Issue(HttpContext, user.UserId, model.RememberMe);
                    return RedirectAfterLogin(string.Equals(roleName, "ADMIN", StringComparison.OrdinalIgnoreCase), model.ReturnUrl);
                }
            }
            catch (Exception ex) when (ex is System.Data.SqlClient.SqlException || ex is InvalidOperationException)
            {
                ModelState.AddModelError("", "Không thể đăng nhập lúc này. Vui lòng thử lại sau."); return View(model);
            }
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            NoCache(); AuthSessions.Revoke(HttpContext);
            if (Session != null) { Session.Clear(); Session.Abandon(); new SessionIDManager().RemoveSessionID(System.Web.HttpContext.Current); }
            return RedirectToAction("Index", "Home");
        }

        private ActionResult RedirectAfterLogin(bool admin, string returnUrl)
        {
            if (admin) return RedirectToAction("Dashboard", "Dashboard");
            if (IsSafeCustomerUrl(returnUrl)) return Redirect(Url.Content(returnUrl));
            return RedirectToAction("Index", "Home");
        }

        private bool IsSafeCustomerUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Url.IsLocalUrl(value)) return false;
            var resolved = Url.Content(value);
            var path = resolved.Split('?')[0].Split('#')[0];
            if (path.IndexOf('\\') >= 0 || path.IndexOf('%') >= 0) return false;
            var app = (Request.ApplicationPath ?? "/").TrimEnd('/');
            if (!path.StartsWith(app + "/", StringComparison.OrdinalIgnoreCase)) return false;
            var relative = path.Substring(app.Length);
            if (relative.IndexOf("..", StringComparison.Ordinal) >= 0) return false;
            return relative == "/" || relative.Equals("/Home", StringComparison.OrdinalIgnoreCase) || relative.StartsWith("/Home/", StringComparison.OrdinalIgnoreCase)
                || relative.Equals("/Book", StringComparison.OrdinalIgnoreCase) || relative.StartsWith("/Book/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidEmail(string value) { try { var email = new MailAddress(value); return string.Equals(email.Address, value, StringComparison.OrdinalIgnoreCase); } catch (FormatException) { return false; } }
        private void NoCache() { Response.Cache.SetCacheability(HttpCacheability.NoCache); Response.Cache.SetNoStore(); }
    }
}
