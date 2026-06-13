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
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(u =>
                    u.Username == model.Username &&
                    u.PasswordHash == model.Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid Username or Password";
                return View(model);
            }

            //HttpContext.Session.SetInt32("UserID", user.UserId);
            //HttpContext.Session.SetString("Username", user.Username);

            //return RedirectToAction("Index", "Home");
            var userRole = _context.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefault(ur => ur.UserId == user.UserId);

            HttpContext.Session.SetInt32("UserID", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);

            if (userRole != null)
            {
                HttpContext.Session.SetInt32("RoleID", userRole.RoleId);
                HttpContext.Session.SetString("RoleName", userRole.Role.RoleName);
                return RedirectToAction("Index", "Home");
            }

            // If no role assigned, block login and show friendly message
            ViewBag.Error = "No role assigned to this account. Contact administrator.";
            return View(model);

            return RedirectToAction("Index", "Home");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}