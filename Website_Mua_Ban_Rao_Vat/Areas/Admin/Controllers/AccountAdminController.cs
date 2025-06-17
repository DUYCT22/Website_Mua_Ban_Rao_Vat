using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using PagedList;
namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class AccountAdminController : BaseController
    {
        Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        // GET: Admin/Account
        public ActionResult Index(string search, string role, bool? status, int page = 1, int pageSize = 5)
        {
            try
            {
                var users = Entity.Users.AsQueryable();
                int currentUserId = (int)Session["UserId"];
                users = users.Where(u => u.Id != currentUserId);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    users = users.Where(u =>
                        u.FullName.Contains(search) ||
                        u.Email.Contains(search) ||
                        u.Phone.Contains(search) ||
                        u.LoginName.Contains(search));
                }
                if (!string.IsNullOrWhiteSpace(role))
                {
                    int roleValue = int.Parse(role);
                    users = users.Where(u => u.Role == roleValue);
                }
                if (status.HasValue)
                {
                    users = users.Where(u => u.Status == status.Value);
                }
                string dateSort = Request["dateSort"];
                if (dateSort == "newest")
                {
                    users = users.OrderByDescending(u => u.CreatedAt);
                }
                else if (dateSort == "oldest")
                {
                    users = users.OrderBy(u => u.CreatedAt);
                }
                else
                {
                    users = users.OrderByDescending(u => u.CreatedAt);
                }
                var pagedUsers = users.ToPagedList(page, pageSize);
                return View(pagedUsers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpPost]
        public ActionResult Lock(int id)
        {
            try
            {
                var user = Entity.Users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                user.Status = false;
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã khóa tài khoản thành công." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpPost]
        public ActionResult Unlock(int id)
        {
            try
            {
                var user = Entity.Users.FirstOrDefault(u => u.Id == id);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                user.Status = true;
                Entity.SaveChanges();
                return Json(new { success = true, message = "Đã mở khóa tài khoản thành công." });
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