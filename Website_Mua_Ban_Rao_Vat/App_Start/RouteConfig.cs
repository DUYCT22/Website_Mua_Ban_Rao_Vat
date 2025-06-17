using CloudinaryDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Linq;

namespace Website_Mua_Ban_Rao_Vat
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "FriendlyHome",
                url: "Trang-chu",
                defaults: new { controller = "Home", action = "Index" }
            );
            routes.MapRoute(
               name: "FriendlyLogin",
               url: "Dang-nhap",
               defaults: new { controller = "Account", action = "Index" }
           );
            routes.MapRoute(
              name: "FriendlyLogout",
              url: "Dang-xuat",
              defaults: new { controller = "Account", action = "Logout" }
          );
            routes.MapRoute(
               name: "FriendlyRegister",
               url: "Dang-ky",
               defaults: new { controller = "Account", action = "Register" }
           );
            routes.MapRoute(
                name: "FriendlyDetail",
                url: "Bai-dang/{slug}-{encryptedId}",
                defaults: new { controller = "Listing", action = "Detail" }
            );
            routes.MapRoute(
              name: "FriendlyCreateListing",
              url: "Dang-tin",
              defaults: new { controller = "Listing", action = "Create" }
          );
            routes.MapRoute(
               name: "FriendlyMessages",
               url: "Tin-nhan",
               defaults: new { controller = "Chat", action = "Messages" }
           );
            routes.MapRoute(
               name: "FriendlyProfile",
               url: "Trang-ca-nhan",
               defaults: new { controller = "User", action = "Profile" }
           );
            routes.MapRoute(
               name: "FriendlyFavorite",
               url: "Yeu-thich",
               defaults: new { controller = "User", action = "Favorite" }
           );
            routes.MapRoute(
               name: "FriendlyLiByCate",
               url: "Danh-muc/{slug}-{encryptedId}",
               defaults: new { controller = "Listing", action = "LiByCate" }
           );
            routes.MapRoute(
              name: "FriendlyAllListing",
              url: "Tat-ca-bai-dang",
              defaults: new { controller = "Listing", action = "Index" }
          );
            routes.MapRoute(
               name: "FriendlySeller",
               url: "Nguoi-ban/{slug}-{encryptedId}",
               defaults: new { controller = "Listing", action = "ProfileSeller" }
           );
            routes.MapRoute(
                name: "FriendlyTerms",
                url: "Dieu-khoan-su-dung",
                defaults: new { controller = "Account", action = "TermsOfUse" }
            );
            routes.MapRoute(
               name: "FriendlyAbout",
               url: "Ve-chung-toi",
               defaults: new { controller = "Home", action = "About" }
           );
            routes.MapRoute(
               name: "FriendlyContact",
               url: "Lien-he",
               defaults: new { controller = "Home", action = "Contact" }
           );
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "Website_Mua_Ban_Rao_Vat.Controllers" }
            );

        }
    }
}
