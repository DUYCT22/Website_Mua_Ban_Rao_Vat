using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using System.Data.Entity;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class ListingController : Controller
    {
        private Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        HomeModel HomeModel = new HomeModel();
        UserModel UserModel = new UserModel();
        [HttpGet]
        public ActionResult Index(int? categoryId, string sort, int page = 1, int pageSize = 12)
        {
            try
            {
                IQueryable<Listing> query = Entity.Listings
                    .Include("User")
                    .Include("Images")
                    .Where(n => n.Status == 1 && n.User.Status == true);

                if (Session["userId"] != null)
                {
                    var id = (int)Session["userId"];
                    query = query.Where(n => n.UserId != id);
                }

                if (categoryId.HasValue && categoryId.Value > 0)
                {
                    List<int> categoryIds = new List<int> { categoryId.Value };
                    var subCategoryIds = Entity.Categories
                        .Where(c => c.ParentId == categoryId.Value)
                        .Select(c => c.Id)
                        .ToList();
                    categoryIds.AddRange(subCategoryIds);
                    query = query.Where(n => n.CategoryId.HasValue && categoryIds.Contains(n.CategoryId.Value));
                }
                switch (sort)
                {
                    case "oldest":
                        query = query.OrderBy(n => n.CreatedAt);
                        break;
                    case "asc":
                        query = query.OrderBy(n => n.Price);
                        break;
                    case "desc":
                        query = query.OrderByDescending(n => n.Price);
                        break;
                    default:
                        query = query.OrderByDescending(n => n.CreatedAt);
                        break;
                }

                var totalCount = query.Count();

                var listings = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var userIds = listings.Select(n => n.UserId).Distinct().ToList();
                var ratings = Entity.Ratings.Where(r => userIds.Contains(r.UserId)).ToList();

                HomeModel.Listing = listings;
                HomeModel.RatingByListing = listings.ToDictionary(
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

                ViewBag.TotalCount = totalCount;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.Categories = GetCategorySelectList();
                return View(HomeModel);
            }
            catch (Exception ex)
            {
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
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["userId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            var id = (int)Session["userId"];
            var user = Entity.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return HttpNotFound();
            }
            if (user.Status == false)
            {
                ViewBag.ErrorMessage = "Bạn không được phép đăng bài do tài khoản đang bị khóa để giải quyết khiếu nại!";
                return View("Error");
            }
            ViewBag.Categories = GetCategorySelectList();
            ViewBag.ParentCategories = new SelectList(Entity.Categories.Where(c => c.ParentId == null), "Id", "Name");
            ViewBag.SubCategories = new SelectList(new List<Category>(), "Id", "Name");
            return View();
        }
        [HttpGet]
        public JsonResult GetSubCategories(int parentId)
        {
            var subCategories = Entity.Categories
                .Where(c => c.ParentId == parentId)
                .Select(c => new { Id = c.Id, Name = c.Name })
                .ToList();

            return Json(subCategories, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetCategories()
        {
            var categories = Entity.Categories
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.ParentId
                }).ToList();

            return Json(categories, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Edit(Listing model)
        {
            if (Session["userId"] == null)
                return Json(new { success = false, message = "Bạn chưa đăng nhập." });
            var listing = Entity.Listings.FirstOrDefault(x => x.Id == model.Id);
            if (listing == null)
                return Json(new { success = false, message = "Không tìm thấy bài đăng." });
            listing.Price = model.Price;
            listing.CategoryId = model.CategoryId;
            listing.UpdatedAt = DateTime.Now;
            Entity.SaveChanges();
            return Json(new { success = true, message = "Đã cập nhật thành công!" });
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(Listing listing, List<HttpPostedFileBase> images)
        {
            try
            {
                if (Session["userId"] == null) return HttpNotFound();
                listing.CreatedAt = DateTime.Now;
                listing.Status = 0;
                listing.UserId = (int)Session["userId"];
                Entity.Listings.Add(listing);
                Entity.SaveChanges();

                if (images != null && images.Any())
                {
                    var cloudService = new CloudinaryService();
                    var fileNames = Entity.Images
                        .Select(i => i.ImageURL)
                        .ToList()
                        .Select(path => Path.GetFileNameWithoutExtension(path))
                        .ToList();
                    var maxNumber = fileNames
                        .Where(name => int.TryParse(name, out _))
                        .Select(name => int.Parse(name))
                        .DefaultIfEmpty(0)
                        .Max();
                    int nextNumber = maxNumber + 1;
                    foreach (var file in images)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string extension = Path.GetExtension(file.FileName);
                            string newFileName = nextNumber + extension;
                            string publicId = "Listings/" + nextNumber;
                            try
                            {
                                var uploadParams = new ImageUploadParams()
                                {
                                    File = new FileDescription(file.FileName, file.InputStream),
                                    PublicId = publicId,
                                    UseFilename = false,
                                    UniqueFilename = false,
                                    Overwrite = false
                                };
                                var result = cloudService.Upload(uploadParams);
                                Entity.Images.Add(new Image
                                {
                                    ListingId = listing.Id,
                                    ImageURL = result.SecureUrl.ToString()
                                });
                            }
                            catch (Exception imgEx)
                            {
                                Console.WriteLine("Lỗi upload ảnh: " + imgEx.Message);  
                            }
                            nextNumber++;
                        }
                    }
                    Entity.SaveChanges();
                }
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        public ActionResult Detail(string slug, string encryptedId)
        {
            if (slug == null || encryptedId == null)
            {
                return RedirectToAction("Index", "Home");
            }
            int? id = IdProtector.DecryptId(encryptedId);
            if (id == null)
                return HttpNotFound();
            try
            {
                ListingModel model = new ListingModel();
                model.Listing = Entity.Listings.Find(id);
                if (model.Listing == null)
                {
                    ViewBag.ErrorMessage = "Bài đăng không tồn tại.";
                    return View("Error");
                }
                model.RelateListing = Entity.Listings
                    .Where(n => n.Category.ParentId == model.Listing.Category.ParentId && n.Id != id)
                    .Take(4)
                    .ToList();
                model.Images = Entity.Images.Where(i => i.ListingId == id).ToList();
                model.User = Entity.Users.Find(model.Listing.UserId);
                model.Rating = Entity.Ratings.Where(n => n.UserId == model.User.Id).ToList();
                if (Session["userId"] != null)
                {
                    var userId = (int)Session["userId"];
                    ViewBag.Follow = Entity.Follows.Where(n => n.FollowerId == userId && n.SellerId == model.User.Id).ToList();
                    ViewBag.Favorite = Entity.Favorites.Where(n => n.UserId == userId && n.ListingId == model.Listing.Id).ToList();
                }
                ViewBag.Categories = GetCategorySelectList();
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpGet]
        public ActionResult LiByCate(string slug, string encryptedId, string sort, int page = 1, int pageSize = 12)
        {
            if (slug == null || encryptedId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            int? id = IdProtector.DecryptId(encryptedId);
            if (id == null)
                return HttpNotFound();

            try
            {
                var categoryId = id.Value;
                var category = Entity.Categories.Find(categoryId);
                if (category == null || ToSlug(category.Name) != slug)
                {
                    return RedirectToAction("Index", "Home");
                }
                var childCategoryIds = Entity.Categories
                    .Where(c => c.ParentId == categoryId)
                    .Select(c => c.Id)
                    .ToList();
                var allCategoryIds = new List<int> { categoryId };
                allCategoryIds.AddRange(childCategoryIds);
                HomeModel.Categories = Entity.Categories
                    .Where(c => c.ParentId == categoryId)
                    .ToList();
                IQueryable<Listing> listingsQuery = Entity.Listings
                    .Include("User")
                    .Include("Images")
                    .Where(n => n.CategoryId.HasValue && allCategoryIds.Contains(n.CategoryId.Value)
                                && n.Status == 1
                                && n.User.Status == true);
                if (Session["userId"] != null)
                {
                    int idUser = (int)Session["userId"];
                    listingsQuery = listingsQuery.Where(n => n.UserId != idUser);
                }
                switch (sort)
                {
                    case "oldest":
                        listingsQuery = listingsQuery.OrderBy(n => n.CreatedAt);
                        break;
                    case "asc":
                        listingsQuery = listingsQuery.OrderBy(n => n.Price);
                        break;
                    case "desc":
                        listingsQuery = listingsQuery.OrderByDescending(n => n.Price);
                        break;
                    default:
                        listingsQuery = listingsQuery.OrderByDescending(n => n.CreatedAt);
                        break;
                }
                ViewBag.TotalCount = listingsQuery.Count();
                HomeModel.Listing = listingsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
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
                ViewBag.PageSize = pageSize;
                ViewBag.Categories = GetCategorySelectList();
                if (Request.IsAjaxRequest())
                {
                    return PartialView("_ListingPartial", HomeModel);
                }
                return View(HomeModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        [HttpPost]
        public JsonResult ShowPhoneNumber(int id)
        {
            if (Session["userId"] == null) return Json(new { success = false, message = "Bạn cần đăng nhập." });
            try
            {
                var userId = (int)Session["userId"];
                var listing = Entity.Listings.Find(id);
                if (listing == null) return Json(new { success = false, message = "Bài đăng không tồn tại." });
                var user = Entity.Users.Find(userId);
                if (user == null) return Json(new { success = false, message = "Người bán không tồn tại." });
                var newNotification = new Notification
                {
                    UserId = listing.UserId,
                    RelatedUserId = userId,
                    ListingId = listing.Id,
                    Message = user.FullName + ", Sđt: " + user.Phone + " đã xem số điện thoại của bạn!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                var notification = Entity.Notifications.Where(n => n.Message == newNotification.Message).ToList();
                if (!notification.Any())
                {
                    Entity.Notifications.Add(newNotification);
                    Entity.SaveChanges();
                }
                return Json(new { success = true, message = "Sđt: " + listing.User.Phone });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult ProfileSeller(string slug, string encryptedId)
        {
            if (slug == null || encryptedId == null)
            {
                return RedirectToAction("Index", "Home");
            }
            int? id = IdProtector.DecryptId(encryptedId);
            if (id == null)
                return HttpNotFound();
            try
            {
                UserModel.user = Entity.Users.FirstOrDefault(n => n.Id == id);
                if (UserModel.user == null)
                {
                    ViewBag.ErrorMessage = "Người bán không tồn tại.";
                    return View("Error");
                }
                if (UserModel.user.Status == false)
                {
                    ViewBag.ErrorMessage = "Người bán này đang bị khóa để giải quyết khiếu nại.";
                    return View("Error");
                }
                UserModel.user = Entity.Users.FirstOrDefault(n => n.Id == id);
                UserModel.listingShowing = Entity.Listings.Where(n => n.Status == 1 && n.UserId == id).ToList();
                UserModel.listingSold = Entity.Listings.Where(n => n.Status == 2 && n.UserId == id).ToList();
                UserModel.follows = Entity.Follows.Where(n => n.SellerId == id).ToList();
                UserModel.Rating = Entity.Ratings.Where(n => n.UserId == id).ToList();
                if (Session["userId"] != null)
                {
                    var userId = (int)Session["userId"];
                    ViewBag.Follow = Entity.Follows.Where(n => n.FollowerId == userId && n.SellerId == UserModel.user.Id).ToList();
                }
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult MarkAsSold(int id)
        {
            try
            {
                var listing = Entity.Listings.Find(id);
                if (listing == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bài đăng." });
                }
                listing.Status = 2;
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã thay đổi trạng thái thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false });
            }
        }
        [HttpPost]
        public JsonResult CreateRating(int userId, int score, string comment)
        {
            try
            {
                if (Session["userId"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để viết đánh giá." });
                }
                int reviewerId = (int)Session["userId"];
                var existing = Entity.Ratings.FirstOrDefault(r => r.UserId == userId && r.ReviewerId == reviewerId);
                var user = Entity.Users.FirstOrDefault(n => n.Id == reviewerId);
                if (existing != null)
                {
                    return Json(new { success = false, message = "Bạn đã đánh giá người này rồi." });
                }
                var rating = new Rating
                {
                    UserId = userId,
                    ReviewerId = reviewerId,
                    Score = score,
                    Comment = comment,
                    CreatedAt = DateTime.Now
                };
                var notification = new Notification
                {
                    UserId = userId,
                    RelatedUserId = reviewerId,
                    Message = user.FullName + " vừa đánh giá bạn " + score + " ⭐",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                Entity.Ratings.Add(rating);
                Entity.Notifications.Add(notification);
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đánh giá thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false });
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

    }
}