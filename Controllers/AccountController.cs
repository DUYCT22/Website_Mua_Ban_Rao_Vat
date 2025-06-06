using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class AccountController : Controller
    {
        //private IAuthenticationManager AuthenticationManager => HttpContext.GetOwinContext().Authentication;
        private Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
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

                if (user.Status == false)
                {
                    return Json(new { success = false, message = "Tài khoản đang bị khóa để giải quyết khiếu nại. Vui lòng liên hệ nguyennhutduyư.cv@gmail.com!" });
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
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
        }
        //public void ExternalLogin(string provider)
        //{
        //    var redirectUrl = Url.Action("ExternalLoginCallback", "Account");
        //    var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        //    AuthenticationManager.Challenge(properties, provider);
        //}

        //public ActionResult ExternalLoginCallback()
        //{
        //    var loginInfo = AuthenticationManager.GetExternalLoginInfo();
        //    if (loginInfo == null)
        //    {
        //        return RedirectToAction("Index");
        //    }
        //    var email = loginInfo.Email;
        //    var name = loginInfo.ExternalIdentity?.Name ?? loginInfo.DefaultUserName;
        //    var existingUser = Entity.Users.FirstOrDefault(u => u.Email == email);
        //    if (existingUser == null)
        //    {
        //        var newUser = new User
        //        {
        //            FullName = name,
        //            Email = email,
        //            Phone = "",
        //            LoginName = email.Split('@')[0],
        //            Password = Guid.NewGuid().ToString(),
        //            Avatar = "https://res.cloudinary.com/dgpw5aart/image/upload/v1747829789/user_ljmbxd.png",
        //            Role = 0,
        //            Online = true,
        //            CreatedAt = DateTime.Now
        //        };
        //        Entity.Users.Add(newUser);
        //        Entity.SaveChanges();
        //        Session["userId"] = newUser.Id;
        //        Session["username"] = newUser.FullName;
        //    }
        //    else
        //    {
        //        Session["userId"] = existingUser.Id;
        //        Session["username"] = existingUser.FullName;
        //    }
        //    return RedirectToAction("Index", "Home");
        //}
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
                user.Status = true;
                user.CreatedAt = DateTime.Now;
                Entity.Users.Add(user);
                Entity.SaveChanges();
                return Json(new
                {
                    success = true,
                    message = "Đăng ký thành công!",
                    redirectUrl = Url.Action("Index", "Account")
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
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
                //AuthenticationManager.SignOut("ApplicationCookie");
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

        public ActionResult TermsOfUse()
        {
            return View();
        }
    }
}