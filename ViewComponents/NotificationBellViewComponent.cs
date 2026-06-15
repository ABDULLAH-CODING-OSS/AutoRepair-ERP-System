using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Html;
using System.Linq;
using AutoRepairERD.Services;

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
                var guestHtml = "<div class=\"nav-item\"><a class=\"nav-link\" href=\"/Notifications\"><i class=\"bi bi-bell\"></i></a></div>";
                return new HtmlContentViewComponentResult(new Microsoft.AspNetCore.Html.HtmlString(guestHtml));
            }

            var uid = uidObj.Value;
            var count = await _notificationService.GetUnreadCountForUserAsync(uid);
            var latest = await _notificationService.GetLatestForUserAsync(uid, 5);

            var sb = new System.Text.StringBuilder();
            sb.Append("<div class=\"nav-item dropdown\">");
            sb.Append("<a class=\"nav-link dropdown-toggle position-relative\" href=\"#\" id=\"notifDropdown\" role=\"button\" data-bs-toggle=\"dropdown\" aria-expanded=\"false\">");
            sb.Append("<i class=\"bi bi-bell\"></i>");
            if (count > 0)
            {
                sb.Append($"<span class=\"position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger\">{count}</span>");
            }
            sb.Append("</a>");
            sb.Append("<ul class=\"dropdown-menu dropdown-menu-end p-2\" aria-labelledby=\"notifDropdown\" style=\"min-width:360px;\">");
            sb.Append("<li class=\"dropdown-header\">Notifications</li>");
            if (latest != null && latest.Any())
            {
                foreach (var n in latest)
                {
                    var title = System.Net.WebUtility.HtmlEncode(n.Title ?? string.Empty);
                    var message = System.Net.WebUtility.HtmlEncode(n.Message ?? string.Empty);
                    var time = n.CreatedAt?.ToString("g") ?? string.Empty;
                    sb.Append("<li class=\"dropdown-item\">");
                    sb.Append("<div class=\"d-flex\"><div class=\"flex-grow-1\">");
                    sb.Append($"<div class=\"d-flex justify-content-between\"><div><strong>{title}</strong><div class=\"small text-muted\">{message}</div></div><div class=\"text-end small text-muted\">{time}</div></div>");
                    sb.Append($"<div class=\"mt-1\"><a href=\"/Notifications/Details/{n.NotificationId}\" class=\"stretched-link text-decoration-none\">View</a></div>");
                    sb.Append("</div></div></li><li><hr class=\"dropdown-divider\" /></li>");
                }
            }
            else
            {
                sb.Append("<li class=\"dropdown-item text-muted\">No notifications</li>");
            }
            sb.Append("<li><a class=\"dropdown-item text-center\" href=\"/Notifications\">View all</a></li>");
            sb.Append("</ul></div>");

            return new HtmlContentViewComponentResult(new Microsoft.AspNetCore.Html.HtmlString(sb.ToString()));
        }
    }
}
