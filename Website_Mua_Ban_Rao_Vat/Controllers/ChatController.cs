﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Website_Mua_Ban_Rao_Vat.Models;
using System.Data.Entity;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
namespace Website_Mua_Ban_Rao_Vat.Controllers
{
    public class ChatController : Controller
    {
        private Mua_Ban_Vao_VatEntities Entity = new Mua_Ban_Vao_VatEntities();
        public ActionResult Messages()
        {
            try
            {
                if (Session["userId"] == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                int currentUserId = (int)Session["userId"];
                var users = Entity.Messages
                    .Include(m => m.User).Include(m => m.User1)
                    .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                    .Select(m => m.SenderId == currentUserId ? m.User : m.User1)
                    .Where(u => u.Id != currentUserId && u.Status == true)
                    .Distinct().ToList();
                ViewBag.ChatUsers = users;
                ViewBag.Title = "Nhắn tin";
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }
        [HttpGet]
        public JsonResult GetChatHistory(int userId)
        {
            try
            {
                int currentUserId = (int)Session["userId"];
                var messagesRaw = Entity.Messages
                    .Include(m => m.Listing.Images)
                    .Where(m => (m.SenderId == currentUserId && m.ReceiverId == userId) ||
                                (m.SenderId == userId && m.ReceiverId == currentUserId) && m.User.Status == true) 
                    .Select(m => new
                    {
                        m.SenderId,
                        m.ReceiverId,
                        m.Content,
                        SentAt = m.SentAt ?? DateTime.MinValue,
                        SenderFullName = m.SenderId == currentUserId ? "Bạn" : m.User1.FullName,
                        ReceiverFullName = m.ReceiverId == currentUserId ? "Bạn" : m.User.FullName,
                        ListingId = m.ListingId,
                        ListingTitle = m.Listing != null ? m.Listing.Title : null,
                        ListingImage = m.Listing.Images.FirstOrDefault().ImageURL
                    }).ToList();
                var messages = messagesRaw.Select(m => new
                {
                    m.SenderId,
                    m.ReceiverId,
                    m.Content,
                    m.SentAt,
                    m.SenderFullName,
                    m.ReceiverFullName,
                    m.ListingId,
                    m.ListingTitle,
                    m.ListingImage,
                    EncryptedId = m.ListingId != null ? IdProtector.EncryptId(m.ListingId.Value) : null
                }).ToList();

                return Json(new { success = true, data = messages }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpPost]
        public async Task<JsonResult> SendMessage(int receiverId, string content, int? listingId)
        {
            try
            {
                if (Session["userId"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để nhắn tin với người bán!" });
                }

                int currentUserId = (int)Session["userId"];

                if (receiverId == currentUserId)
                {
                    return Json(new { success = false, message = "Bạn không thể gửi tin nhắn cho chính mình." });
                }

                if (string.IsNullOrWhiteSpace(content) && listingId.HasValue)
                {
                    content = "";
                }

                if (string.IsNullOrWhiteSpace(content) && !listingId.HasValue)
                {
                    return Json(new { success = false, message = "Không thể gửi tin nhắn rỗng." });
                }
                var user = await Entity.Users.FirstOrDefaultAsync(x => x.Id == receiverId);

                if (user == null)
                {
                    return Json(new { success = false, message = "Người nhận không tồn tại." });
                }

                if (user.Status == false)
                {
                    return Json(new { success = false, message = "Không thể gửi tin nhắn cho người này vì họ đã bị khóa." });
                }
                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    ListingId = listingId,
                    Content = content,
                    SentAt = DateTime.Now
                };

                Entity.Messages.Add(message);
                Entity.SaveChanges();
                string conversationId = currentUserId < receiverId
                    ? $"{currentUserId}_{receiverId}"
                    : $"{receiverId}_{currentUserId}";

                var firebaseData = new
                {
                    SenderId = currentUserId,
                    ReceiverId = receiverId,
                    Content = content,
                    SentAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    SenderFullName = Entity.Users.Find(currentUserId)?.FullName ?? "Bạn"
                };


                string json = JsonConvert.SerializeObject(firebaseData);
                string firebaseUrl = $"https://muabanraovat-63362-default-rtdb.firebaseio.com/messages/{conversationId}.json";

                using (var httpClient = new HttpClient())
                {
                    var contentHttp = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(firebaseUrl, contentHttp);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Lỗi khi gửi Firebase: " + response.StatusCode);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<JsonResult> SendMessageAdmin( string content, int? listingId)
        {
            try
            {
                if (Session["userId"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để nhắn tin với quản trị viên!" });
                }

                int currentUserId = (int)Session["userId"];

                if (string.IsNullOrWhiteSpace(content) && listingId.HasValue)
                {
                    content = "";
                }

                if (string.IsNullOrWhiteSpace(content) && !listingId.HasValue)
                {
                    return Json(new { success = false, message = "Không thể gửi tin nhắn rỗng." });
                }
                var user = await Entity.Users.FirstOrDefaultAsync(x => x.Role == 1);

                if (user == null)
                {
                    return Json(new { success = false, message = "Người nhận không tồn tại." });
                }

                if (user.Status == false)
                {
                    return Json(new { success = false, message = "Không thể gửi tin nhắn cho người này vì họ đã bị khóa." });
                }
                var message = new Message
                {
                    SenderId = currentUserId,
                    ReceiverId = user.Id,
                    ListingId = listingId,
                    Content = content,
                    SentAt = DateTime.Now
                };

                Entity.Messages.Add(message);
                Entity.SaveChanges();
                string conversationId = currentUserId < user.Id
                    ? $"{currentUserId}_{user.Id}"
                    : $"{user.Id}_{currentUserId}";

                var firebaseData = new
                {
                    SenderId = currentUserId,
                    ReceiverId = user.Id,
                    Content = content,
                    SentAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    SenderFullName = Entity.Users.Find(currentUserId)?.FullName ?? "Bạn"
                };


                string json = JsonConvert.SerializeObject(firebaseData);
                string firebaseUrl = $"https://muabanraovat-63362-default-rtdb.firebaseio.com/messages/{conversationId}.json";

                using (var httpClient = new HttpClient())
                {
                    var contentHttp = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(firebaseUrl, contentHttp);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Lỗi khi gửi Firebase: " + response.StatusCode);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, StackTrace: {ex.StackTrace}");
                ViewBag.ErrorMessage = ex.Message;
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}