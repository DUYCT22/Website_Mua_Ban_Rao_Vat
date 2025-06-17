using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using System.Configuration;
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
        private static Dictionary<string, (string Code, DateTime Expire)> resetCodes = new Dictionary<string, (string Code, DateTime Expire)>();
        [HttpPost]
        public JsonResult SendResetCode(string email)
        {
            var user = Entity.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
                return Json(new { success = false, message = "Email không tồn tại." });

            var code = new Random().Next(100000, 999999).ToString();
            resetCodes[email] = (code, DateTime.Now.AddMinutes(5));
            MailHelper.Send(email, "Mã xác nhận đặt lại mật khẩu", $"Mã của bạn là: <b>{code}</b>");

            return Json(new { success = true });
        }
        [HttpPost]
        public JsonResult VerifyResetCode(string email, string code)
        {
            if (resetCodes.ContainsKey(email))
            {
                var (storedCode, expireTime) = resetCodes[email];
                if (storedCode == code && DateTime.Now <= expireTime)
                {
                    return Json(new { success = true });
                }
            }
            return Json(new { success = false });
        }
        [HttpPost]
        public JsonResult ResetPassword(string email, string password)
        {
            var user = Entity.Users.FirstOrDefault(u => u.Email == email);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(password);
            Entity.SaveChanges();
            if (resetCodes.ContainsKey(email)) resetCodes.Remove(email);

            return Json(new { success = true });
        }
        public static class MailHelper
        {
            public static void Send(string toEmail, string subject, string body)
            {
                var fromEmail = new MailAddress("nguyennhutduy.cv@gmail.com", "Tin Tốt");
                var to = new MailAddress(toEmail);
                var password = ConfigurationManager.AppSettings["EmailPassword"];
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail.Address, password)
                };
                using (var message = new MailMessage(fromEmail, to)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                })
                    smtp.Send(message);
            }
        }
        [HttpPost]
        public JsonResult ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            try
            {
                var userId = (int?)Session["userId"];
                if (userId == null || userId == 0)
                {
                    return Json(new { success = false, message = "Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại." });
                }

                var user = Entity.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null || !BCrypt.Net.BCrypt.Verify(OldPassword, user.Password))
                {
                    return Json(new { success = false, message = "Mật khẩu cũ không đúng." });
                }
                if (NewPassword != ConfirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu mới không trùng khớp." });
                }
                try
                {
                    MailHelper.Send(user.Email, "Tài khoản của bạn vừa thay đổi mật khẩu!", "Nếu bạn không thực hiện hành động đó vui lòng liên hệ nguyennhutduy.cv@gmail.com");
                }
                catch (Exception mailEx)
                {
                    Console.WriteLine("Lỗi gửi mail: " + mailEx.Message);
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                Entity.SaveChanges();

                return Json(new { success = true, message = "Đổi mật khẩu thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }
        [HttpPost]
        public JsonResult Send(string name, string email, string message)
        {
            try
            {
                string subject = "Liên hệ từ khách hàng - Tin Tốt";
                string body = $@"
                <h3>Thông tin liên hệ</h3>
                <p><strong>Họ tên:</strong> {name}</p>
                <p><strong>Email:</strong> {email}</p>
                <p><strong>Nội dung:</strong><br />{message}</p>
                <p>Thời gian gửi: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</p>";
                MailHelper.Send("nguyennhutduy.cv@gmail.com", subject, body);
                return Json(new { success = true, message = "Cảm ơn bạn đã liên hệ. Chúng tôi sẽ phản hồi sớm nhất!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }
    }
}