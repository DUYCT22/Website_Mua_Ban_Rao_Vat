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
        Mua_Ban_Vao_Vat_Entities Entity = new Mua_Ban_Vao_Vat_Entities();
        HomeModel HomeModel = new HomeModel();
        [HttpGet]
        public ActionResult Create()
        {
            if (Session["userId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.SubCategories = new SelectList(Entity.Categories.Where(c => c.ParentId != null), "Id", "Name");
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(Listing listing, List<HttpPostedFileBase> images)
        {
            try
            {
                listing.CreatedAt = DateTime.Now;
                listing.Status = 1;
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
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
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
                    .Where(n => n.CategoryId == model.Listing.CategoryId && n.Id != id)
                    .Take(4)
                    .ToList();
                model.Images = Entity.Images.Where(i => i.ListingId == id).ToList();
                model.User = Entity.Users.Find(model.Listing.UserId);
                model.Rating = Entity.Ratings.Where(n => n.UserId == model.User.Id).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return View("Error");
            }
        }
        public ActionResult LiByCate(string slug, string encryptedId)
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
                HomeModel.Categories = Entity.Categories.Where(n => n.ParentId == id).ToList();
                if (Session["userId"] != null)
                {
                    var idUser = (int)Session["userId"];
                    HomeModel.Listing = Entity.Listings
                    .Include("User").Include("Images")
                    .Where(n => n.UserId != idUser && n.Category.ParentId == id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();
                }
                else
                {
                    if (HomeModel.Categories.Count != 0)
                    {
                        HomeModel.Listing = Entity.Listings
                        .Include("User").Include("Images")
                        .Where(n => n.Category.ParentId == id)
                        .OrderByDescending(n => n.CreatedAt)
                        .ToList();
                    }
                    else
                    {
                        HomeModel.Listing = Entity.Listings
                       .Include("User").Include("Images")
                       .Where(n => n.CategoryId == id)
                       .OrderByDescending(n => n.CreatedAt)
                       .ToList();
                    }

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
    }
}