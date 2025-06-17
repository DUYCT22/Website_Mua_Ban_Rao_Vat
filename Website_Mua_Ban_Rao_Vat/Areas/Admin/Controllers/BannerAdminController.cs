using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using PagedList;
using System.IO;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Website_Mua_Ban_Rao_Vat.Controllers;
using System.Data.Entity;
namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class BannerAdminController : BaseController
    {
        Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        public ActionResult Index(string searchString, int? page, string sort)
        {
            try
            {
                int pageSize = 3;
                int pageNumber = (page ?? 1);
                var banners = Entity.Banners.AsQueryable();
                if (sort == "asc")
                {
                    banners = banners.OrderBy(n => n.CreatedAt);
                }
                else if (sort == "desc")
                {
                    banners = banners.OrderByDescending(n => n.CreatedAt);
                }
                else
                {
                    banners = banners.OrderBy(n => n.Id);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    banners = banners.Where(b => b.Link.Contains(searchString));
                    ViewBag.CurrentFilter = searchString;
                }

                var data = banners.OrderBy(n => n.Id).ToPagedList(pageNumber, pageSize);
                return View(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpPost]
        public ActionResult Create(string Link, HttpPostedFileBase ImageFile)
        {
            try
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileNames = Entity.Banners
                          .Select(i => i.Image)
                          .ToList()
                          .Select(path => Path.GetFileNameWithoutExtension(path))
                          .ToList();
                    var maxNumber = fileNames
                        .Where(name => int.TryParse(name, out _))
                        .Select(name => int.Parse(name))
                        .DefaultIfEmpty(0)
                        .Max();

                    int nextNumber = maxNumber + 1;
                    string extension = Path.GetExtension(ImageFile.FileName);
                    string newFileName = nextNumber.ToString() + extension;
                    var cloudinary = new CloudinaryService();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(ImageFile.FileName, ImageFile.InputStream),
                        PublicId = $"Banner/{nextNumber}",
                        UseFilename = false,
                        UniqueFilename = false,
                        Overwrite = true
                    };
                    var uploadResult = cloudinary.Upload(uploadParams);
                    int maxOrder = Entity.Banners.Any()? Entity.Banners.Max(b => b.Orders ?? 0): 0;
                    var banner = new Banner
                    {
                        Link = Link,
                        Image = uploadResult.SecureUrl.ToString(),
                        Orders = maxOrder + 1, 
                        Status = true,
                        CreatedAt = DateTime.Now,
                        CreatedBy = (int)Session["userId"]
                    };
                    Entity.Banners.Add(banner);
                    Entity.SaveChanges();
                }
                return RedirectToAction("Index");
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
        public JsonResult Edit(int Id, string Link, HttpPostedFileBase ImageFile)
        {
            try
            {
                var banner = Entity.Banners.Find(Id);
                if (banner == null)
                    return Json(new { success = false, message = "Không tìm thấy banner!" });

                banner.Link = Link;
                banner.UpdatedAt = DateTime.Now;
                banner.UpdatedBy = (int)Session["userId"];
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var cloudinaryService = new CloudinaryService();
                    string publicId = null;

                    if (!string.IsNullOrEmpty(banner.Image))
                    {
                        var uri = new Uri(banner.Image);
                        var segments = uri.AbsolutePath.Split(new[] { "/upload/" }, StringSplitOptions.None);
                        if (segments.Length > 1)
                        {
                            var publicIdWithExtension = segments[1]; // "v123456/Banner/1.jpg"
                            var parts = publicIdWithExtension.Split('/');
                            if (parts.Length >= 2)
                            {
                                publicId = string.Join("/", parts.Skip(1)); // "Banner/1.jpg"
                                publicId = Path.ChangeExtension(publicId, null); // "Banner/1"
                                cloudinaryService.Delete(new DeletionParams(publicId));
                            }
                        }
                    }

                    publicId = publicId ?? Guid.NewGuid().ToString();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(ImageFile.FileName, ImageFile.InputStream),
                        PublicId = publicId,
                        Overwrite = true,
                        Folder = "Banner"
                    };
                    var uploadResult = cloudinaryService.Upload(uploadParams);
                    banner.Image = uploadResult.SecureUrl.ToString();
                }

                Entity.SaveChanges();

                return Json(new
                {
                    success = true,
                    bannerUrl = banner.Image,
                    message = "Đã thay đổi banner thành công!"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Delete(int Id)
        {
            try
            {
                var banner = Entity.Banners.Find(Id);
                if (banner != null)
                {
                    var cloudinaryService = new CloudinaryService();
                    if (!string.IsNullOrEmpty(banner.Image))
                    {
                        var uri = new Uri(banner.Image);
                        var segments = uri.AbsolutePath.Split(new[] { "/upload/" }, StringSplitOptions.None);
                        if (segments.Length > 1)
                        {
                            var publicIdWithExtension = segments[1]; // "v123456/Banner/1.jpg"
                            var parts = publicIdWithExtension.Split('/');
                            if (parts.Length >= 2)
                            {
                                var publicId = string.Join("/", parts.Skip(1)); // "Banner/1.jpg"
                                publicId = Path.ChangeExtension(publicId, null); // "Banner/1"
                                cloudinaryService.Delete(new DeletionParams(publicId));
                            }
                        }
                    }

                    Entity.Banners.Remove(banner);
                    Entity.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        [HttpPost]
        public ActionResult Off(int id)
        {
            try
            {
                var banner = Entity.Banners.FirstOrDefault(u => u.Id == id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner." });
                }
                banner.Status = false;
                banner.UpdatedAt = DateTime.Now;
                banner.UpdatedBy = (int)Session["userId"];
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã tắt banner thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpPost]
        public ActionResult On(int id)
        {
            try
            {
                var banner = Entity.Banners.FirstOrDefault(u => u.Id == id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner." });
                }
                banner.Status = true;
                banner.UpdatedAt = DateTime.Now;
                banner.UpdatedBy = (int)Session["userId"];
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã bật banner thành công." });
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
        public JsonResult ChangeOrder(ChangeOrderRequest request)
        {
            try
            {
                var banner = Entity.Banners.FirstOrDefault(b => b.Id == request.Id);
                if (banner == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy banner." });
                }

                if (!banner.Orders.HasValue)
                {
                    return Json(new { success = false, message = "Banner chưa có thứ tự." });
                }
                int oldOrder = banner.Orders.Value;
                int newOrder = request.NewOrder;

                if (oldOrder == newOrder)
                {
                    return Json(new { success = false, message = "Thứ tự không thay đổi." });
                }

                if (newOrder > oldOrder)
                {
                    var items = Entity.Banners
                        .Where(b => b.Orders > oldOrder && b.Orders <= newOrder)
                        .ToList();
                    foreach (var b in items)
                    {
                        b.Orders--;
                    }
                }
                else
                {
                    var items = Entity.Banners
                        .Where(b => b.Orders >= newOrder && b.Orders < oldOrder)
                        .ToList();
                    foreach (var b in items)
                    {
                        b.Orders++;
                    }
                }

                banner.Orders = newOrder;
                banner.UpdatedAt = DateTime.Now;
                banner.UpdatedBy = (int)Session["userId"];
                Entity.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        public class ChangeOrderRequest
        {
            public int Id { get; set; }
            public int NewOrder { get; set; }
        }

    }
}