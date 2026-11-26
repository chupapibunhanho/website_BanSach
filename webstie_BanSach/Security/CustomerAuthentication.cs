using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Mvc.Filters;
using System.Web.Security;
using webstie_BanSach.Models;

namespace webstie_BanSach.Security
{
    public sealed class CustomerPrincipal : GenericPrincipal
    {
        public string DisplayName { get; private set; }
        public CustomerPrincipal(long id, string name, string role) : base(new GenericIdentity(id.ToString(), "Forms"), new[] { role }) { DisplayName = name; }
    }

    public static class AuthSessions
    {
        private static readonly object Sync = new object();
        private const string SessionPrefix = "auth:session:";
        private sealed class Attempt { public int Count; public DateTime UntilUtc; }
        private static string Digest(string value) { using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""))); }
        private static string AccountKey(string email) { return "auth:account:" + Digest((email ?? "").ToLowerInvariant()); }
        private static string IpKey(string ip) { return "auth:ip:" + Digest(ip ?? ""); }
        private static bool Blocked(string key, int limit) { var state = HttpRuntime.Cache[key] as Attempt; return state != null && state.UntilUtc > DateTime.UtcNow && state.Count >= limit; }
        public static bool IsLimited(string email, string ip) { lock (Sync) return Blocked(AccountKey(email), 5) || Blocked(IpKey(ip), 30); }
        private static void Increment(string key) {
            var now = DateTime.UtcNow; var state = HttpRuntime.Cache[key] as Attempt;
            if (state == null || state.UntilUtc <= now) state = new Attempt { UntilUtc = now.AddMinutes(5) };
            state.Count++; HttpRuntime.Cache.Insert(key, state, null, state.UntilUtc, System.Web.Caching.Cache.NoSlidingExpiration);
        }
        public static void Failed(string email, string ip) { lock (Sync) { Increment(AccountKey(email)); Increment(IpKey(ip)); } }
        public static void Succeeded(string email) { lock (Sync) HttpRuntime.Cache.Remove(AccountKey(email)); }
        public static bool FixedTimeEquals(string left, string right) {
            var a = Encoding.UTF8.GetBytes(left ?? ""); var b = Encoding.UTF8.GetBytes(right ?? "");
            var diff = a.Length ^ b.Length; var max = Math.Max(a.Length, b.Length);
            for (var i = 0; i < max; i++) diff |= (i < a.Length ? a[i] : 0) ^ (i < b.Length ? b[i] : 0);
            return diff == 0 && a.Length > 0;
        }
        public static void Issue(HttpContextBase context, long userId, bool remember) {
            Revoke(context); var bytes = new byte[32]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            var nonce = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var now = DateTime.Now; var expires = now.Add(remember ? TimeSpan.FromDays(14) : TimeSpan.FromHours(8));
            HttpRuntime.Cache.Insert(SessionPrefix + nonce, userId, null, expires.ToUniversalTime(), System.Web.Caching.Cache.NoSlidingExpiration);
            var ticket = new FormsAuthenticationTicket(1, userId.ToString(), now, expires, remember, nonce, FormsAuthentication.FormsCookiePath);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket)) {
                HttpOnly = true, Secure = context.Request.IsSecureConnection, SameSite = SameSiteMode.Lax, Path = FormsAuthentication.FormsCookiePath
            };
            if (remember) cookie.Expires = expires;
            context.Response.Cookies.Add(cookie);
        }
        public static void Revoke(HttpContextBase context) {
            var cookie = context.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie != null) { try { var ticket = FormsAuthentication.Decrypt(cookie.Value); if (ticket != null && !string.IsNullOrEmpty(ticket.UserData)) HttpRuntime.Cache.Remove(SessionPrefix + ticket.UserData); } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is System.Security.Cryptography.CryptographicException || ex is HttpException) { } }
            FormsAuthentication.SignOut();
        }
        public static bool Valid(string nonce, long id) { var stored = HttpRuntime.Cache[SessionPrefix + (nonce ?? "")]; return stored is long && (long)stored == id; }
    }

    public sealed class CustomerAuthenticationFilter : IAuthenticationFilter
    {
        public void OnAuthentication(AuthenticationContext context) {
            IPrincipal principal = new GenericPrincipal(new GenericIdentity(""), new string[0]);
            var cookie = context.HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie != null) {
                try {
                    var ticket = FormsAuthentication.Decrypt(cookie.Value);
                    if (ticket != null && !ticket.Expired && long.TryParse(ticket.Name, out var id) && AuthSessions.Valid(ticket.UserData, id)) {
                        using (var db = new DatabaseDataContext()) {
                            var user = (from u in db.Users join r in db.Roles on u.RoleId equals r.RoleId
                                        where u.UserId == id && u.IsActive select new { u.UserId, u.FullName, r.RoleName }).FirstOrDefault();
                            if (user != null && (string.Equals(user.RoleName, "ADMIN", StringComparison.OrdinalIgnoreCase) || string.Equals(user.RoleName, "CUSTOMER", StringComparison.OrdinalIgnoreCase)))
                                principal = new CustomerPrincipal(user.UserId, user.FullName, user.RoleName.ToUpperInvariant());
                        }
                    }
                } catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is System.Security.Cryptography.CryptographicException || ex is HttpException || ex is System.Data.SqlClient.SqlException || ex is InvalidOperationException) { }
            }
            context.Principal = principal; context.HttpContext.User = principal; Thread.CurrentPrincipal = principal;
        }
        public void OnAuthenticationChallenge(AuthenticationChallengeContext context) { }
    }

    public sealed class AntiForgeryErrorFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context) {
            if (!(context.Exception is HttpAntiForgeryException)) return;
            context.HttpContext.Response.TrySkipIisCustomErrors = true;
            context.Result = new HttpStatusCodeResult(400, "Invalid request");
            context.ExceptionHandled = true;
        }
    }

    public sealed class AdminAccessAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase context) { return context.User != null && context.User.Identity.IsAuthenticated && context.User.IsInRole("ADMIN"); }
        public override void OnAuthorization(AuthorizationContext context) {
            base.OnAuthorization(context);
            context.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache); context.HttpContext.Response.Cache.SetNoStore();
            if (context.Result != null || !string.Equals(context.HttpContext.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase)) return;
            try { var cookie = context.HttpContext.Request.Cookies[AntiForgeryConfig.CookieName]; AntiForgery.Validate(cookie == null ? null : cookie.Value, context.HttpContext.Request.Headers["RequestVerificationToken"]); }
            catch (HttpAntiForgeryException) { context.HttpContext.Response.TrySkipIisCustomErrors = true; context.Result = new HttpStatusCodeResult(400); }
        }
        protected override void HandleUnauthorizedRequest(AuthorizationContext context) {
            var response = context.HttpContext.Response; response.TrySkipIisCustomErrors = true; response.SuppressFormsAuthenticationRedirect = true;
            if (context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated) { context.Result = new HttpStatusCodeResult(403); return; }
            if (context.HttpContext.Request.IsAjaxRequest() || !string.Equals(context.HttpContext.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase)) { context.Result = new HttpStatusCodeResult(401); return; }
            var url = new UrlHelper(context.RequestContext); context.Result = new RedirectResult(url.Action("Login", "Account", new { returnUrl = context.HttpContext.Request.RawUrl }));
        }
    }
}
