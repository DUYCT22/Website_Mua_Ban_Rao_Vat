using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;

namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class AccountController : Controller
    {
        Mua_Ban_Vao_Vat_Entities Entity = new Mua_Ban_Vao_Vat_Entities();
        [HttpGet]
        public ActionResult Index()
        {
            if (Session["userId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public ActionResult Index(User model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.LoginName) || string.IsNullOrEmpty(model.Password))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin." });
                }

                var user = Entity.Users
                    .FirstOrDefault(u => u.LoginName == model.LoginName || u.Email == model.LoginName || u.Phone == model.LoginName);

                if (user == null)
                {
                    return Json(new { success = false, message = "Tài khoản không tồn tại." });
                }

                if (!BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    return Json(new { success = false, message = "Mật khẩu không đúng." });
                }

                user.Online = true;
                Entity.SaveChanges();
                Session["userId"] = user.Id;
                Session["userName"] = user.FullName;
                Session["userAvatar"] = user.Avatar;

                return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = Url.Action("Index", "Home") });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        [HttpGet]
        public ActionResult Register()
        {
            if (Session["userId"] != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [HttpPost]
        public ActionResult Register(User user, HttpPostedFileBase Avatar)
        {
            try
            {
                bool isEmailExist = Entity.Users.Any(u => u.Email == user.Email);
                bool isUsernameExist = Entity.Users.Any(u => u.LoginName == user.LoginName);
                bool isPhoneExist = Entity.Users.Any(u => u.Phone == user.Phone);
                if (isEmailExist || isUsernameExist || isPhoneExist)
                {
                    string msg = "";
                    if (isEmailExist) msg += "Email đã tồn tại. ";
                    if (isUsernameExist) msg += "Tên đăng nhập đã tồn tại. ";
                    if (isPhoneExist) msg += "Số điện thoại đã tồn tại.";

                    TempData["Notification"] = msg;
                    TempData["NotificationType"] = "error";
                    return View(user);
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

                if (Avatar != null && Avatar.ContentLength > 0)
                {
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
                    string extension = Path.GetExtension(Avatar.FileName);
                    string newFileName = nextNumber.ToString() + extension;
                    var cloudinary = new CloudinaryService();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(Avatar.FileName, Avatar.InputStream),
                        PublicId = $"User/{nextNumber}",
                        UseFilename = false,
                        UniqueFilename = false,
                        Overwrite = true
                    };
                    var uploadResult = cloudinary.Upload(uploadParams);
                    user.Avatar = uploadResult.SecureUrl.ToString();
                }
                user.Online = false;
                user.Role = 0;
                user.CreatedAt = DateTime.Now;
                Entity.Users.Add(user);
                Entity.SaveChanges();
                TempData["Notification"] = "Đăng ký thành công!";
                TempData["NotificationType"] = "success";
                return RedirectToAction("Index", "Account");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                return View("Error");
            }
        }
        public ActionResult Logout()
        {
            if (Session["userId"] == null)
            {
                return RedirectToAction("Index", "Home");
            }
            try
            {
                int id = (int)Session["userId"];
                var user = Entity.Users.FirstOrDefault(a => a.Id == id);
                user.Online = false;
                Entity.SaveChanges();
                Session.Clear();
                return RedirectToAction("Index", "Home");
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