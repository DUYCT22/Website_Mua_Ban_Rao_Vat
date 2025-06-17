using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Website_Mua_Ban_Rao_Vat.Models;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System.IO;
using PagedList;
using PagedList.Mvc;
using Website_Mua_Ban_Rao_Vat.Controllers;
using System.Reflection;
using System.Web.Services.Description;

namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class CategoryAdminController : BaseController
    {
        // GET: Admin/Category
        Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        public ActionResult Index(string searchString, int? parentId, int? page, string sort)
        {
            try
            {
                int pageSize = 4;
                int pageNumber = (page ?? 1);
                var categories = Entity.Categories.AsQueryable();
                if (sort == "asc")
                {
                    categories = categories.OrderBy(n => n.CreatedAt);
                }
                else if (sort == "desc")
                {
                    categories = categories.OrderByDescending(n => n.CreatedAt);
                }
                else
                {
                    categories = categories.OrderBy(n => n.Id);
                }
                if (!string.IsNullOrEmpty(searchString))
                {
                    categories = categories.Where(c => c.Name.Contains(searchString));
                }
                if (parentId.HasValue)
                {
                    categories = categories.Where(c => c.ParentId == parentId.Value);
                }
                categories = categories.OrderBy(n => n.Id);
                ViewBag.CurrentFilter = searchString;
                ViewBag.CurrentParentId = parentId;
                ViewBag.Categories = Entity.Categories.Where(c => c.ParentId == null).ToList();
                return View(categories.ToPagedList(pageNumber, pageSize));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpPost]
        public ActionResult Create(string Name, int? ParentId, HttpPostedFileBase ImageFile)
        {
            try
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var fileNames = Entity.Categories
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
                        PublicId = $"Category/{nextNumber}",
                        UseFilename = false,
                        UniqueFilename = false,
                        Overwrite = true
                    };
                    var uploadResult = cloudinary.Upload(uploadParams);
                    var category = new Category
                    {
                        Name = Name,
                        ParentId = ParentId,
                        Image = uploadResult.SecureUrl.ToString(),
                        CreatedAt = DateTime.Now,
                        CreatedBy = (int)Session["userId"]
                    };
                    Entity.Categories.Add(category);
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
        public JsonResult Edit(int Id, string Name, int? ParentId, HttpPostedFileBase ImageFile)
        {
            try
            {
                var category = Entity.Categories.Find(Id);
                if (category == null)
                    return Json(new { success = false, message = "Không tìm thấy danh mục!" });

                category.Name = Name;
                category.UpdatedAt = DateTime.Now;
                category.UpdatedBy = (int)Session["userId"];
                if (category.ParentId != null)
                {
                    category.ParentId = ParentId;
                }
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var cloudinaryService = new CloudinaryService();
                    string publicId = null;

                    if (!string.IsNullOrEmpty(category.Image))
                    {
                        var uri = new Uri(category.Image);
                        var segments = uri.AbsolutePath.Split(new[] { "/upload/" }, StringSplitOptions.None);
                        if (segments.Length > 1)
                        {
                            var publicIdWithExtension = segments[1]; // "v123456/Category/1.jpg"
                            var parts = publicIdWithExtension.Split('/');
                            if (parts.Length >= 2)
                            {
                                publicId = string.Join("/", parts.Skip(1)); // "Category/1.jpg"
                                publicId = Path.ChangeExtension(publicId, null); // "Category/1"
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
                        Folder = "Category"
                    };
                    var uploadResult = cloudinaryService.Upload(uploadParams);
                    category.Image = uploadResult.SecureUrl.ToString();
                }

                Entity.SaveChanges();

                return Json(new
                {
                    success = true,
                    bannerUrl = category.Image,
                    message = "Đã thay đổi danh mục thành công!"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}