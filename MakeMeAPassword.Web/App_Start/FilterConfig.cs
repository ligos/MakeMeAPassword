using System.Web;
using System.Web.Mvc;

namespace MurrayGrant.PasswordGenerator.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
#if !DEBUG
            //filters.Add(new RequireHttpsAttribute());
#endif
        }
    }
}