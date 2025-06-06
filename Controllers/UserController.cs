using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System.IO;
using System.Reflection;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class UserController : Controller
    {
        private Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
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
                UserModel.listingPendingApproval = Entity.Listings.Where(n => n.Status == 0 && n.UserId == id).ToList();
                UserModel.listingShowing = Entity.Listings.Where(n => n.Status == 1 && n.UserId == id).ToList();
                UserModel.listingSold = Entity.Listings.Where(n => n.Status == 2 && n.UserId == id).ToList();
                UserModel.follows = Entity.Follows.Where(n => n.SellerId == id).ToList();
                UserModel.Rating = Entity.Ratings.Where(n => n.UserId == id).ToList();
                ViewBag.Categories = GetCategorySelectList();
                return View(UserModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
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
                ViewBag.Categories = GetCategorySelectList();
                return View(listFavorite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
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
                var listing = Entity.Listings.Find(id);
                var user = Entity.Users.Find(userId);
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
                var newNotification = new Notification
                {
                    UserId = listing.UserId,
                    RelatedUserId = userId,
                    ListingId = listing.Id,
                    Message = "❤️ " + user.FullName + ", Sđt: " + user.Phone + " đã lưu tin " + listing.Title + " của bạn!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                Entity.Favorites.Add(newFavorite);
                Entity.Notifications.Add(newNotification);
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã lưu thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult RemoveFavoriteAjax(int id, string type)
        {
            try
            {
                if(type == "favorite")
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
                else if(type == "listing")
                {
                    var listing = Entity.Listings.Find(id);
                    if (listing != null)
                    {
                        var userId = (int)Session["userId"];
                        var favorite = Entity.Favorites.FirstOrDefault(n => n.UserId == userId && n.ListingId == listing.Id);
                        if (favorite != null)
                        {
                            Entity.Favorites.Remove(favorite);
                            Entity.SaveChanges();
                            return Json(new { success = true });
                        }
                        return Json(new { success = true });
                    }
                    return Json(new { success = false, message = "Không tìm thấy tin đã lưu." });
                }
                return Json(new { success = false, message = "Kiểu không hợp lệ!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Follow(int id, string type)
        {
            if (Session["userId"] == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }
            try
            {
                var userId = (int)Session["userId"];
                if(type == "listing")
                {
                    var listing = Entity.Listings.Find(id);
                    var user = Entity.Users.Find(userId);
                    if (listing == null || user == null)
                    {
                        return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                    }
                    var folow = Entity.Follows.Where(n => n.FollowerId == userId && n.SellerId == listing.UserId);
                    if (!folow.Any())
                    {

                        var newFollow = new Follow
                        {
                            FollowerId = userId,
                            SellerId = listing.UserId,
                            CreatedAt = DateTime.Now,
                        };
                        var newNotification = new Notification
                        {
                            UserId = listing.UserId,
                            RelatedUserId = userId,
                            Message = "👁 " + user.FullName + " đã theo dõi bạn",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                        };
                        Entity.Follows.Add(newFollow);
                        Entity.Notifications.Add(newNotification);
                        Entity.SaveChanges();
                        return Json(new { success = true, message = "Bạn đã theo dõi người bán này!" });
                    }
                    return Json(new { success = false, message = "Bạn đang theo dõi người bán này!" });
                }else if (type == "profile")
                {
                    var profile = Entity.Users.Find(id);
                    var user = Entity.Users.Find(userId);
                    if (profile == null || user == null)
                    {
                        return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                    }
                    var folow = Entity.Follows.Where(n => n.FollowerId == userId && n.SellerId == profile.Id);
                    if (!folow.Any())
                    {

                        var newFollow = new Follow
                        {
                            FollowerId = userId,
                            SellerId = profile.Id,
                            CreatedAt = DateTime.Now,
                        };
                        var newNotification = new Notification
                        {
                            UserId = profile.Id,
                            RelatedUserId = userId,
                            Message = user.FullName + " đã theo dõi bạn",
                            IsRead = false,
                            CreatedAt = DateTime.Now,
                        };
                        Entity.Follows.Add(newFollow);
                        Entity.Notifications.Add(newNotification);
                        Entity.SaveChanges();
                        return Json(new { success = true, message = "Bạn đã theo dõi người bán này!" });
                    }
                    return Json(new { success = false, message = "Bạn đang theo dõi người bán này!" });
                }
                return Json(new { success = false, message = "Có lỗi xảy ra!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UnFollow(int id, string type)
        {
            if (Session["userId"] == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }
            try
            {
                var userId = (int)Session["userId"];
                if(type == "listing")
                {
                    var listing = Entity.Listings.Find(id);
                    var user = Entity.Users.Find(userId);
                    if (listing == null || user == null)
                    {
                        return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                    }
                    Follow folow = Entity.Follows.FirstOrDefault(n => n.FollowerId == userId && n.SellerId == listing.UserId);
                    if (folow != null)
                    {
                        Entity.Follows.Remove(folow);
                        Entity.SaveChanges();
                        return Json(new { success = true, message = "Bạn đã hủy theo dõi người bán này!" });
                    }
                    return Json(new { success = false, message = "Bạn chưa theo dõi người bán này!" });
                }else if (type == "profile")
                {
                    var profile = Entity.Users.Find(id);
                    var user = Entity.Users.Find(userId);
                    if (profile == null || user == null)
                    {
                        return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                    }
                    Follow folow = Entity.Follows.FirstOrDefault(n => n.FollowerId == userId && n.SellerId == profile.Id);
                    if (folow != null)
                    {
                        Entity.Follows.Remove(folow);
                        Entity.SaveChanges();
                        return Json(new { success = true, message = "Bạn đã hủy theo dõi người bán này!" });
                    }
                    return Json(new { success = false, message = "Bạn chưa theo dõi người bán này!" });
                }
                return Json(new { success = false, message = "Có lỗi xảy ra!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult ChangeAvatar(HttpPostedFileBase Avatar)
        {
            if (Session["userId"] == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập." });
            }
            try
            {
                if (Avatar != null && Avatar.ContentLength > 0)
                {
                    var cloudinaryService = new CloudinaryService();
                    int userId = (int)Session["UserId"];
                    var user = Entity.Users.Find(userId);

                    if (user == null)
                        return Json(new { success = false, message = "Không tìm thấy người dùng." });
                    string publicId = null;
                    if (!string.IsNullOrEmpty(user.Avatar))
                    {
                        var uri = new Uri(user.Avatar);
                        var segments = uri.AbsolutePath.Split(new[] { "/upload/" }, StringSplitOptions.None);
                        if (segments.Length > 1)
                        {
                            var publicIdWithExtension = segments[1]; // "v123456/User/1.jpg"
                            var parts = publicIdWithExtension.Split('/');
                            if (parts.Length >= 2)
                            {
                                publicId = string.Join("/", parts.Skip(1)); // "User/1.jpg"
                                publicId = Path.ChangeExtension(publicId, null); // "User/1"
                                cloudinaryService.Delete(new DeletionParams(publicId));
                            }
                        }
                    }
                    publicId = publicId ?? Guid.NewGuid().ToString();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(Avatar.FileName, Avatar.InputStream),
                        PublicId = publicId,
                        Overwrite = true,
                        Folder = "User"
                    };
                    var uploadResult = cloudinaryService.Upload(uploadParams);
                    user.Avatar = uploadResult.SecureUrl.ToString();
                    Session["userAvatar"] = uploadResult.SecureUrl.ToString();
                    Entity.SaveChanges();
                    return Json(new { success = true, avatarUrl = user.Avatar, message = "Đã thay đổi ảnh đại diện thành công" });
                }
                return Json(new { success = false, message = "Chưa chọn ảnh để upload." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}