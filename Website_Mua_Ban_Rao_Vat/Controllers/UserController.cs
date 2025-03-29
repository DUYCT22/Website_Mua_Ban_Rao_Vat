using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class UserController : Controller
    {
        Mua_Ban_Vao_Vat_Entities Entity = new Mua_Ban_Vao_Vat_Entities();
        UserModel UserModel = new UserModel();
        public ActionResult Profile()
        {
            try
            {
                if (Session["userId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var id = (int)Session["userId"];
                UserModel.user = Entity.Users.FirstOrDefault(n => n.Id == id);
                UserModel.listingShowing = Entity.Listings.Where(n => n.Status == 1 && n.UserId == id).ToList();
                UserModel.listingSold = Entity.Listings.Where(n => n.Status == 2 && n.UserId == id).ToList();
                UserModel.follows = Entity.Follows.Where(n => n.SellerId == id).ToList();
                UserModel.Rating = Entity.Ratings.Where(n => n.UserId == id).ToList();
                return View(UserModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return View("Error");
            }
        }
        public ActionResult Favorite()
        {
            try
            {
                if (Session["userId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                var id = (int)Session["userId"];
                var listFavorite = Entity.Favorites
                    .Include(f => f.Listing)
                    .Include(f => f.Listing.Images)
                    .Where(f => f.UserId == id)
                    .ToList();
                return View(listFavorite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return View("Error");
            }
        }
        [HttpPost]
        public JsonResult AddFavorite(int id)
        {
            if (Session["userId"] == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }
            try
            {
                var userId = (int)Session["userId"];
                var exists = Entity.Favorites.Any(f => f.UserId == userId && f.ListingId == id);
                if (exists)
                {
                    return Json(new { success = false, message = "Đã có trong danh sách lưu." });
                }
                var newFavorite = new Favorite
                {
                    UserId = userId,
                    ListingId = id,
                    CreatedAt = DateTime.Now
                };
                Entity.Favorites.Add(newFavorite);
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã lưu thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RemoveFavoriteAjax(int id)
        {
            try
            {
                var favorite = Entity.Favorites.Find(id);
                if (favorite != null)
                {
                    Entity.Favorites.Remove(favorite);
                    Entity.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy tin đã lưu." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}