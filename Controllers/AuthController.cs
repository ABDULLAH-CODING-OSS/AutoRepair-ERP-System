using AutoRepairERD.Models;
using AutoRepairERD.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoRepairERD.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var vm = new ViewModels.LoginViewModel();
            // Populate roles dropdown so user can select role on the same screen
            vm.Roles = _context.Roles
                .OrderBy(r => r.RoleName)
                .Select(r => System.Tuple.Create(r.RoleId, r.RoleName))
                .ToList();
            return View(vm);
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // repopulate roles for the view on validation failure
                model.Roles = _context.Roles.OrderBy(r => r.RoleName).Select(r => System.Tuple.Create(r.RoleId, r.RoleName)).ToList();
                return View(model);
            }

            var user = _context.Users
                .FirstOrDefault(u =>
                    u.Username == model.Username &&
                    u.PasswordHash == model.Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid Username or Password";
                model.Roles = _context.Roles.OrderBy(r => r.RoleName).Select(r => System.Tuple.Create(r.RoleId, r.RoleName)).ToList();
                return View(model);
            }

            // Load roles assigned to this user
            var roles = _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.UserId)
                .Select(ur => new { ur.RoleId, ur.Role.RoleName })
                .ToList();

            HttpContext.Session.SetInt32("UserID", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);

            if (roles.Count == 0)
            {
                ViewBag.Error = "No role assigned to this account. Contact administrator.";
                return View(model);
            }

            // If user selected a role on the same form, validate assignment and login
            if (model.SelectedRoleId.HasValue)
            {
                var assignment = _context.UserRoles.FirstOrDefault(ur => ur.UserId == user.UserId && ur.RoleId == model.SelectedRoleId.Value);
                if (assignment == null)
                {
                    ModelState.AddModelError(string.Empty, "Selected role is not assigned to this account.");
                    model.Roles = roles.Select(r => System.Tuple.Create(r.RoleId, r.RoleName)).ToList();
                    return View(model);
                }

                HttpContext.Session.SetInt32("UserID", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("RoleID", assignment.RoleId);
                HttpContext.Session.SetString("RoleName", _context.Roles.Find(assignment.RoleId)?.RoleName ?? string.Empty);
                return RedirectToAction("Index", "Home");
            }

            // No role selected on form: if only one assigned role, set it; otherwise prompt selection
            if (roles.Count == 1)
            {
                HttpContext.Session.SetInt32("RoleID", roles[0].RoleId);
                HttpContext.Session.SetString("RoleName", roles[0].RoleName);
                HttpContext.Session.SetInt32("UserID", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                return RedirectToAction("Index", "Home");
            }

            // Multiple roles and none selected: return view with assigned roles for user to choose
            var loginVm = new LoginViewModel
            {
                Username = user.Username,
                Roles = roles.Select(r => System.Tuple.Create(r.RoleId, r.RoleName)).ToList()
            };
            return View("Login", loginVm);
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult SelectRole(RoleSelectionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SelectRole", model);
            }

            // Verify role belongs to user
            var assignment = _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefault(ur => ur.UserId == model.UserId && ur.RoleId == model.SelectedRoleId);

            if (assignment == null)
            {
                // Role not assigned - show error on login screen
                ModelState.AddModelError(string.Empty, "Selected role is not assigned to this account.");
                var roles = _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.UserId == model.UserId)
                    .Select(ur => System.Tuple.Create(ur.RoleId, ur.Role.RoleName))
                    .ToList();

                var loginVm = new LoginViewModel
                {
                    Username = _context.Users.Find(model.UserId)?.Username ?? string.Empty,
                    Roles = roles
                };
                return View("Login", loginVm);
            }

            HttpContext.Session.SetInt32("RoleID", assignment.RoleId);
            HttpContext.Session.SetString("RoleName", assignment.Role.RoleName);

            return RedirectToAction("Index", "Home");
        }
    }
}