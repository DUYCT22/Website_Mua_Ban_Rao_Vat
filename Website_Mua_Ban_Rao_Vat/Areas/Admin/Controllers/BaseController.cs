using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Session["userId"] == null || Session["userRole"] == null)
            {
                filterContext.Result = RedirectToAction("Index", "UserAdmin");
            }
            base.OnActionExecuting(filterContext);
        }
    }
}