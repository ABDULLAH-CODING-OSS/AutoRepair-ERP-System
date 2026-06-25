using AutoRepairERD.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AutoRepairERD.Controllers
{
    public class HomeController : Controller
    {
        //public IActionResult Index()
        //{
        //    return View();
        //}
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            var roleName = HttpContext.Session.GetString("RoleName");

            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            switch (roleName)
            {
                case "Owner":
                    return RedirectToAction("Owner", "Dashboard");
                case "Admin":
                    return RedirectToAction("Admin", "Dashboard");
            }

            ViewBag.Username = HttpContext.Session.GetString("Username");
            ViewBag.RoleName = roleName;
            ViewData["Title"] = "Home";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

    public IActionResult AccessDenied()
    {
        return View();
    }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
