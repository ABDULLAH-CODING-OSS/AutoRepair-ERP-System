using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoRepairERD.Filters
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Auth",
                    null);
            }
        }
    }
}