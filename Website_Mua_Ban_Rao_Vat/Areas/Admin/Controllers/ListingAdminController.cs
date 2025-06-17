using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using PagedList;
using System.Data.Entity;
using System.Web.Razor.Tokenizer.Symbols;
using System.Web.Services.Description;
using OfficeOpenXml;
using System.IO;
using System.Net;
using System.Drawing;
using OfficeOpenXml.Style;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Website_Mua_Ban_Rao_Vat.Controllers;
using CloudinaryDotNet.Actions;
namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class ListingAdminController : BaseController
    {
        Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        public ActionResult BrowseListing()
        {
            try
            {
                var data = Entity.Listings.Include(n => n.Images)
                     .Where(n => n.Status == 0)
                     .OrderBy(n => n.Id).ToList();
                return View(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpGet]
        public JsonResult GetPendingListings()
        {
            try
            {
                var rawData = Entity.Listings
                .Include(l => l.Images)
                .Include(l => l.Category)
                .Include(l => l.User)
                .Where(l => l.Status == 0)
                .OrderBy(l => l.Id)
                .ToList();
                var data = rawData.Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.Description,
                    l.Location,
                    CreatedAt = l.CreatedAt.HasValue ? l.CreatedAt.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa cập nhật",
                    Images = l.Images.Select(i => i.ImageURL).ToList(),
                    Category = l.Category?.Name,
                    User = l.User?.FullName
                }).ToList();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }

        }
        [HttpPost]
        public JsonResult Browse(int id)
        {
            try
            {
                var listing = Entity.Listings.Find(id);
                if (listing != null)
                {
                    listing.Status = 1;
                    var adminId = (int)Session["userId"];
                    var newNotification = new Notification
                    {
                        UserId = listing.UserId,
                        RelatedUserId = adminId,
                        Message = "✔️ Bài đăng " + listing.Title + " đã được phê duyệt!",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                    };
                    Entity.Notifications.Add(newNotification);
                    Entity.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        [HttpGet]
        public ActionResult ExportToExcel(string keyword, string sort, int? categoryId, int? status)
        {
            var listings = Entity.Listings
        .Include("Images")
        .Include("Category")
        .Include("User")
        .AsQueryable();
            if (!string.IsNullOrEmpty(keyword))
            {
                listings = listings.Where(l => l.Title.Contains(keyword));
            }
            if (categoryId.HasValue)
            {
                listings = listings.Where(l => l.CategoryId == categoryId.Value);
            }
            if (status.HasValue)
            {
                listings = listings.Where(l => l.Status == status.Value);
            }
            if (sort == "asc")
            {
                listings = listings.OrderBy(l => l.CreatedAt);
            }
            else
            {
                listings = listings.OrderByDescending(l => l.CreatedAt);
            }
            var filteredListings = listings.ToList();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Danh sách bài đăng");

                worksheet.Cells.Style.Font.Name = "Times New Roman";
                worksheet.Cells.Style.Font.Size = 14;

                worksheet.Cells["A1"].Value = "Tiêu đề";
                worksheet.Cells["B1"].Value = "Hình ảnh";
                worksheet.Cells["C1"].Value = "Chi tiết";
                worksheet.Cells["D1"].Value = "Địa chỉ";
                worksheet.Cells["E1"].Value = "Ngày tạo";
                worksheet.Cells["F1"].Value = "Danh mục";
                worksheet.Cells["G1"].Value = "Người đăng";

                using (var header = worksheet.Cells["A1:G1"])
                {
                    header.Style.Font.Bold = true;
                    header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    header.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    header.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                worksheet.Row(1).Height = 25;
                worksheet.Column(2).Width = 15;
                worksheet.Column(3).Width = 40;
                worksheet.Column(4).Width = 25;
                worksheet.Column(5).Width = 20;
                worksheet.Column(6).Width = 20;
                worksheet.Column(7).Width = 20;

                int row = 2;
                foreach (var item in listings)
                {
                    worksheet.Cells[row, 1].Value = item.Title;

                    if (item.Images != null && item.Images.Any())
                    {
                        try
                        {
                            using (var client = new WebClient())
                            {
                                byte[] imageBytes = client.DownloadData(item.Images.First().ImageURL);
                                using (var stream = new MemoryStream(imageBytes))
                                using (var image = System.Drawing.Image.FromStream(stream))
                                {
                                    var picture = worksheet.Drawings.AddPicture($"Image_{row}", image);
                                    picture.SetPosition(row - 1, 5, 1, 5);
                                    picture.SetSize(80, 80);
                                }
                            }
                        }
                        catch
                        {
                            worksheet.Cells[row, 2].Value = "Không tải được ảnh";
                        }
                    }
                    string plainText = Regex.Replace(item.Description ?? "", "<.*?>", string.Empty);
                    worksheet.Cells[row, 3].Value = plainText;
                    worksheet.Cells[row, 4].Value = item.Location;
                    worksheet.Cells[row, 5].Value = item.CreatedAt?.ToString("dd/MM/yyyy HH:mm");
                    worksheet.Cells[row, 6].Value = item.Category?.Name;
                    worksheet.Cells[row, 7].Value = item.User?.FullName;
                    row++;
                }
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var streamResult = new MemoryStream(package.GetAsByteArray());
                string excelName = $"DanhSachBaiDang_{DateTime.Now:dd/MM/yyyy}.xlsx";
                return File(streamResult, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var listing = Entity.Listings.Find(id);
                if (listing == null)
                    return Json(new { success = false, message = "Không tìm thấy bài đăng." });
                var images = Entity.Images.Where(i => i.ListingId == id).ToList();
                var cloudService = new CloudinaryService();
                foreach (var img in images)
                {
                    var publicId = "Listings/" + Path.GetFileNameWithoutExtension(img.ImageURL);
                    try
                    {
                        var deletionParams = new DeletionParams(publicId);
                        cloudService.Delete(deletionParams);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Lỗi xóa ảnh Cloudinary: " + e.Message);
                    }
                }
                Entity.Images.RemoveRange(images);
                Entity.Listings.Remove(listing);
                var adminId = (int)Session["userId"];
                var newNotification = new Notification
                {
                    UserId = listing.UserId,
                    RelatedUserId = adminId,
                    Message = "❌ Bài đăng \"" + listing.Title + "\" đã bị xóa do vi phạm điều khoản sử dụng!",
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                };
                Entity.Notifications.Add(newNotification);
                Entity.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        public ActionResult ListingShow(int? page, string keyword, string sort, int? categoryId)
        {
            try
            {
                int pageSize = 3;
                int pageNumber = (page ?? 1);

                var query = Entity.Listings
                    .Include(n => n.Images)
                    .Include(n => n.Category)
                    .Include(n => n.User)
                    .Where(n => n.Status == 1);

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(n => n.Title.Contains(keyword));
                }

                if (categoryId.HasValue)
                {
                    var category = Entity.Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (category != null)
                    {
                        if (category.ParentId == null)
                        {
                            var subCategoryIds = Entity.Categories
                                .Where(c => c.ParentId == categoryId)
                                .Select(c => c.Id)
                                .ToList();

                            query = query.Where(n => n.CategoryId.HasValue && subCategoryIds.Contains(n.CategoryId.Value));

                        }
                        else
                        {
                            query = query.Where(n => n.CategoryId == categoryId);
                        }
                    }
                }

                if (sort == "asc")
                {
                    query = query.OrderBy(n => n.CreatedAt);
                }
                else if (sort == "desc")
                {
                    query = query.OrderByDescending(n => n.CreatedAt);
                }
                else
                {
                    query = query.OrderBy(n => n.Id);
                }

                var pagedData = query.ToPagedList(pageNumber, pageSize);
                ViewBag.Categories = Entity.Categories.ToList();

                return View(pagedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        public ActionResult ListingSold(int? page, string keyword, string sort, int? categoryId)
        {
            try
            {
                int pageSize = 3;
                int pageNumber = (page ?? 1);

                var query = Entity.Listings
                    .Include(n => n.Images)
                    .Include(n => n.Category)
                .Include(n => n.User)
                    .Where(n => n.Status == 2);

                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(n => n.Title.Contains(keyword));
                }

                if (categoryId.HasValue)
                {
                    var category = Entity.Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (category != null)
                    {
                        if (category.ParentId == null)
                        {
                            var subCategoryIds = Entity.Categories
                                .Where(c => c.ParentId == categoryId)
                                .Select(c => c.Id)
                                .ToList();

                            query = query.Where(n => n.CategoryId.HasValue && subCategoryIds.Contains(n.CategoryId.Value));

                        }
                        else
                        {
                            query = query.Where(n => n.CategoryId == categoryId);
                        }
                    }
                }

                if (sort == "asc")
                {
                    query = query.OrderBy(n => n.CreatedAt);
                }
                else if (sort == "desc")
                {
                    query = query.OrderByDescending(n => n.CreatedAt);
                }
                else
                {
                    query = query.OrderBy(n => n.Id);
                }

                var pagedData = query.ToPagedList(pageNumber, pageSize);
                ViewBag.Categories = Entity.Categories.ToList();

                return View(pagedData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpPost]
        public JsonResult Hide(int id)
        {
            try
            {
                var listing = Entity.Listings.Find(id);
                if (listing != null)
                {
                    listing.Status = 3;
                    var adminId = (int)Session["userId"];
                    var newNotification = new Notification
                    {
                        UserId = listing.UserId,
                        RelatedUserId = adminId,
                        Message = "🔎 Bài đăng " + listing.Title + " của bạn đã bị ẩn để xem xét!",
                        IsRead = false,
                        CreatedAt = DateTime.Now,
                    };
                    Entity.Notifications.Add(newNotification);
                    Entity.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        public ActionResult ListingHide(int? page)
        {
            try
            {
                int pageSize = 3;
                int pageNumber = (page ?? 1);
                var data = Entity.Listings.Include(n => n.Images)
                         .Where(n => n.Status == 3)
                         .OrderBy(n => n.Id)
                         .ToPagedList(pageNumber, pageSize);
                return View(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
    }
}