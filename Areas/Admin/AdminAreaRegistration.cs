using System.Web.Mvc;

namespace Website_Mua_Ban_Rao_Vat.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
               "Admin_Account",
               "Quan-ly-tai-khoan",
               new { controller = "AccountAdmin", action = "Index" }
           );
            context.MapRoute(
              "Admin_Chat",
              "Tin-nhan-quan-tri",
              new { controller = "ChatAdmin", action = "Messages" }
          );
            context.MapRoute(
               "Admin_Category",
               "Quan-ly-danh-muc",
               new { controller = "CategoryAdmin", action = "Index" }
           );
            context.MapRoute(
                "Admin_Banner",
                "Quan-ly-Banner",
                new { controller = "BannerAdmin", action = "Index" }
            );
            context.MapRoute(
                "Admin_ListingHide",
                "Tin-an",
                new { controller = "ListingAdmin", action = "ListingHide" }
            );
            context.MapRoute(
                "Admin_ListingSold",
                "Tin-da-ban",
                new { controller = "ListingAdmin", action = "ListingSold" }
            );
            context.MapRoute(
                "Admin_ListingShow",
                "Tin-hien",
                new { controller = "ListingAdmin", action = "ListingShow" }
            );
            context.MapRoute(
                "Admin_Browse",
                "Duyet-tin",
                new { controller = "ListingAdmin", action = "BrowseListing" }
            );
            context.MapRoute(
                "Admin_Login",
                "Dang-nhap-quan-tri",
                new { controller = "UserAdmin", action = "Index" }
            );
            context.MapRoute(
                "Admin_Logout",
                "Dang-xuat-quan-tri",
                new { controller = "UserAdmin", action = "Logout" }
            );
            context.MapRoute(
                "Admin_HomeAdmin",
                "Trang-quan-tri",
                new { controller = "HomeAdmin", action = "Index" }
            );
            context.MapRoute(
                "Admin_default",
                "Admin/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers" }
            );
        }
    }
}