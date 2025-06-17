using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;

namespace Website_Mua_Ban_Rao_Vat.Areas.Admin.Controllers
{
    public class HomeAdminController : BaseController
    {
        Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        public ActionResult Index()
        {
            try
            {
                var id = (int)Session["userId"];
                var today = DateTime.Today;
                var lastWeek = today.AddDays(-7);
                var weekBeforeLast = lastWeek.AddDays(-7);
                var thisMonthStart = new DateTime(today.Year, 1, 1);

                var currentWeekListings = Entity.Listings.Count(l => l.CreatedAt >= lastWeek && l.CreatedAt < today);
                var previousWeekListings = Entity.Listings.Count(l => l.CreatedAt >= weekBeforeLast && l.CreatedAt < lastWeek);
                var growthPost = previousWeekListings == 0 ? 100 : ((double)(currentWeekListings - previousWeekListings) / previousWeekListings) * 100;

                var currentWeekUsers = Entity.Users.Count(u => u.CreatedAt >= lastWeek && u.CreatedAt < today);
                var previousWeekUsers = Entity.Users.Count(u => u.CreatedAt >= weekBeforeLast && u.CreatedAt < lastWeek);
                var growthUser = previousWeekUsers == 0 ? 100 : ((double)(currentWeekUsers - previousWeekUsers) / previousWeekUsers) * 100;

                ViewBag.GrowthPost = Math.Round(growthPost, 2);
                ViewBag.GrowthUser = Math.Round(growthUser, 2);
                ViewBag.TotalSoldListings = Entity.Listings.Count(l => l.Status == 2);
                ViewBag.TotalHiddenListings = Entity.Listings.Count(l => l.Status == 3);

                ViewBag.TotalListings = Entity.Listings.Count();
                ViewBag.TotalUsers = Entity.Users.Count();
                ViewBag.PendingListings = Entity.Listings.Count(l => l.Status == 0);
                ViewBag.TotalCategories = Entity.Categories.Count();

                Session["Chat"] = Entity.Messages
                        .Where(m => m.ReceiverId == id || m.SenderId == id)
                        .Select(m => new
                        {
                            User1 = m.SenderId < m.ReceiverId ? m.SenderId : m.ReceiverId,
                            User2 = m.SenderId < m.ReceiverId ? m.ReceiverId : m.SenderId
                        }).Distinct().Count();

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        public JsonResult GetMonthlyListingGrowth()
        {
            var currentYear = DateTime.Now.Year;
            var data = Enumerable.Range(1, 12).Select(month =>
            {
                var count = Entity.Listings.Count(l => l.CreatedAt.Value.Year == currentYear && l.CreatedAt.Value.Month == month);
                return new { Month = month, Count = count };
            }).ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetListingStatusDistribution()
        {
            var total = Entity.Listings.Count();
            var showing = Entity.Listings.Count(l => l.Status == 1); // hiển thị
            var hidden = Entity.Listings.Count(l => l.Status == 3);  // ẩn
            var pending = Entity.Listings.Count(l => l.Status == 0); // chờ duyệt

            return Json(new
            {
                Showing = showing,
                Hidden = hidden,
                Pending = pending,
                Total = total
            }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetListingStatsByCategory()
        {
            try
            {
                var stats = Entity.Listings
                .Where(l => l.Status == 1)
                .GroupBy(l => l.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    Count = g.Count()
                }).ToList();
                return Json(stats, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);

            }
        }

    }
}