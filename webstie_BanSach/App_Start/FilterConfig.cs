using System.Web;
using System.Web.Mvc;

namespace webstie_BanSach
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new webstie_BanSach.Security.CustomerAuthenticationFilter());
            filters.Add(new webstie_BanSach.Security.AntiForgeryErrorFilter());
        }
    }
}
