using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class HomeController : Controller
    {
        Mua_Ban_Vao_Vat_Entities Entity = new Mua_Ban_Vao_Vat_Entities();
        HomeModel HomeModel = new HomeModel();
        public ActionResult Index()
        {
            try
            {
                HomeModel.Bans = Entity.Banners.ToList();
                HomeModel.Categories = Entity.Categories.Where(n => n.ParentId == null).ToList();
                if (Session["userId"] != null)
                {
                    var id = (int)Session["userId"];
                    HomeModel.Listing = Entity.Listings
                    .Include("User").Include("Images")
                    .Where(n => n.UserId != id)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .ToList();
                }
                else
                {
                    HomeModel.Listing = Entity.Listings
                    .Include("User").Include("Images")
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .ToList();
                }
                var userIds = HomeModel.Listing.Select(n => n.UserId).Distinct().ToList();
                var ratings = Entity.Ratings
                    .Where(r => userIds.Contains(r.UserId))
                    .ToList();
                HomeModel.RatingByListing = HomeModel.Listing.ToDictionary(
                    listing => listing.Id,
                    listing => ratings.Where(r => r.UserId == listing.UserId).ToList()
                );
                HomeModel.UserRatingAvg = ratings
                    .Where(r => r.Score.HasValue)
                    .GroupBy(r => r.UserId)
                    .ToDictionary(
                        g => g.Key ?? 0,
                        g => g.Average(r => r.Score.Value)
                );
                return View(HomeModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return View("Error");
            }
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}