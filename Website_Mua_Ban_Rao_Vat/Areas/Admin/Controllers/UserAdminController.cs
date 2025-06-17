using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using System.Web.Helpers;

namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class UserAdminController : Controller
    {
        Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        public ActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public JsonResult Index(User model)
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

                if (user.Role == 2 || user.Role == 0)
                {
                    return Json(new { success = false, message = "Bạn không có quyền truy cập" });
                }
                user.Online = true;
                Entity.SaveChanges();
                Session["userId"] = user.Id;
                Session["userRole"] = user.Role;
                Session["userName"] = user.FullName;
                Session["userAvatar"] = user.Avatar;
                return Json(new { success = true, message = "Đăng nhập thành công!", redirectUrl = Url.Action("Index", "HomeAdmin") });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại sau." });
            }
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
                var password = "btbhrkpyqdbbkhqx";

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
        public ActionResult Logout()
        {
            try
            {
                int id = (int)Session["userId"];
                var user = Entity.Users.FirstOrDefault(a => a.Id == id);
                user.Online = false;
                Entity.SaveChanges();
                Session.Clear();
                return RedirectToAction("Index", "UserAdmin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
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
    }
}