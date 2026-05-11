using System;
using itpayroll.Data;
using itpayroll.Services;
using itpayroll.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace itpayroll.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(NotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Json(new { count = 0 });
            var count = await _notificationService.GetUnreadCount(userId);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent(int count = 5)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Json(Array.Empty<object>());
            var notifications = await _notificationService.GetUserNotifications(userId, count);
            return Json(notifications.Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Message,
                n.IsRead,
                CreatedDate = n.CreatedDate.ToString("MMM dd, HH:mm")
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsRead(id);
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
                await _notificationService.MarkAllAsRead(userId);
            return Json(new { success = true });
        }
    }
}
