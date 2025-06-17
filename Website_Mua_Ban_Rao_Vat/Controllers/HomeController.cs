using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using System.Data.Entity;
namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class HomeController : Controller
    {
        private Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        HomeModel HomeModel = new HomeModel();
        public ActionResult Index()
        {
            try
            {
                HomeModel.Bans = Entity.Banners.Where(n => n.Status == true).OrderBy(n => n.Orders).ToList();
                HomeModel.Categories = Entity.Categories.Where(n => n.ParentId == null).ToList();
                if (Session["userId"] != null)
                {
                    var id = (int)Session["userId"];
                    HomeModel.Listing = Entity.Listings
                    .Include("User").Include("Images")
                    .Where(n => n.UserId != id && n.Status == 1 && n.User.Status == true)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(6)
                    .ToList();
                    Session["Chat"] = Entity.Messages
                        .Where(m => m.ReceiverId == id || m.SenderId == id)
                        .Where(m =>
                            (m.SenderId == id && m.User1.Status == true) &&
                            (m.ReceiverId == id && m.User.Status == true)
                        )
                        .Select(m => new
                        {
                            User1 = m.SenderId < m.ReceiverId ? m.SenderId : m.ReceiverId,
                            User2 = m.SenderId < m.ReceiverId ? m.ReceiverId : m.SenderId
                        }).Distinct().Count();
                    Session["Notifycation"] = Entity.Notifications.Where(n => n.UserId == id).Count();

                }
                else
                {
                    HomeModel.Listing = Entity.Listings
                    .Include("User").Include("Images")
                    .Where(n => n.Status == 1 && n.User.Status == true)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(6)
                    .ToList();
                }
                ViewBag.Categories = GetCategorySelectList();
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
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        public string ToSlug(string str)
        {
            return str.ToLower()
                      .Replace(" ", "-")
                      .Replace("/", "")
                      .Replace("(", "")
                      .Replace(")", "")
                      .Replace("+", "")
                      .Replace(".", "")
                      .Replace(",", "")
                      .Replace("?", "")
                      .Replace("!", "")
                      .Replace(":", "")
                      .Replace(";", "")
                      .Replace("đ", "d")
                      .Normalize(System.Text.NormalizationForm.FormD)
                      .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                      .Aggregate("", (s, c) => s + c)
                      .Replace("--", "-");
        }
        [HttpGet]
        public JsonResult SearchJson(string keyword, int? categoryId)
        {
            var listings = Entity.Listings.Include(l => l.Images).Include(l => l.Category).AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.ToLower();
                listings = listings.Where(l => l.Title != null && l.Title.ToLower().Contains(keyword));
            }

            if (categoryId.HasValue)
            {
                var allCats = Entity.Categories.ToList();
                var selectedAndChildren = allCats
                    .Where(c => c.Id == categoryId.Value || c.ParentId == categoryId.Value)
                    .Select(c => c.Id)
                    .ToList();

                listings = listings.Where(l => l.CategoryId.HasValue && selectedAndChildren.Contains(l.CategoryId.Value));
            }

            var result = listings
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .ToList()
                .Select(l => new
                {
                    id = l.Id,
                    title = l.Title,
                    price = l.Price ?? 0,
                    imageUrl = l.Images.FirstOrDefault()?.ImageURL ?? "/images/no-image.png",
                    slug = ToSlug(l.Title),
                    encryptedId = IdProtector.EncryptId(l.Id)
                });

            return Json(result, JsonRequestBehavior.AllowGet);
        }
        private List<SelectListItem> GetCategorySelectList()
        {
            var allCats = Entity.Categories.ToList();
            List<SelectListItem> items = new List<SelectListItem>();

            foreach (var cat in allCats.Where(c => c.ParentId == null))
            {
                items.Add(new SelectListItem { Value = cat.Id.ToString(), Text = cat.Name });

                foreach (var child in allCats.Where(c => c.ParentId == cat.Id))
                {
                    items.Add(new SelectListItem { Value = child.Id.ToString(), Text = "-- " + child.Name });
                }
            }

            return items;
        }
        [HttpGet]
        public ActionResult NotificationList()
        {
            try
            {
                if (Session == null || Session["userId"] == null)
                    return View("Index", "Home");

                var userId = (int)Session["userId"];
                var notifications = Entity.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(20)
                    .ToList();

                return PartialView("~/Views/Shared/_NotificationList.cshtml", notifications);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return PartialView("~/Views/Shared/_NotificationList.cshtml", new List<Notification>());
            }
        }
        public ActionResult About()
        {
            return View();
        }
        public ActionResult Contact()
        {
            return View();
        }
    }
}