using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoRepairERD.Services;
using AutoRepairERD.ViewModels;

namespace AutoRepairERD.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly NotificationService _notificationService;

        public NotificationBellViewComponent(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var uidObj = HttpContext.Session.GetInt32("UserID");
            if (uidObj == null)
            {
                return View("_EmptyBell");
            }

            var uid = uidObj.Value;
            var vm = new NotificationBellViewModel
            {
                UnreadCount = await _notificationService.GetUnreadCountForUserAsync(uid),
                Latest = await _notificationService.GetLatestForUserAsync(uid, 5)
            };

            return View(vm);
        }
    }
}
