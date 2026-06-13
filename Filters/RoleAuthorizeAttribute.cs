using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AutoRepairERD.Filters
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserID");
            var roleName = context.HttpContext.Session.GetString("RoleName");

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // If roles were specified, ensure the current session role is present and allowed
            if (_roles.Length > 0)
            {
                if (string.IsNullOrEmpty(roleName))
                {
                    // No role in session -> access denied
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                    return;
                }

                // Case-insensitive check
                var allowed = _roles.Any(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
                if (!allowed)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}