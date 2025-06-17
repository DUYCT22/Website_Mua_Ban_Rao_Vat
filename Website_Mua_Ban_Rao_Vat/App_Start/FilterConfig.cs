using System.Web;
using System.Web.Mvc;

namespace Website_Mua_Ban_Rao_Vat
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
