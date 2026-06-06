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
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Auth",
                    null);

                return;
            }

            if (_roles.Length > 0 &&
                !_roles.Contains(roleName))
            {
                context.Result = new ContentResult
                {
                    Content = "Access Denied"
                };

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}