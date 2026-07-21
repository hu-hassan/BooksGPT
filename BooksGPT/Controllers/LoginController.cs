using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BooksGPT.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using log4net;

namespace BooksGPT.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoginController> _logger;
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public LoginController(AppDbContext context, ILogger<LoginController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Login
        public IActionResult Index()
        {
            // Always return the login view. Navigation to Home will be handled when the user
            // clicks OK in the success popup after a successful login.
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            // Find user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            int sizeofpassword = 0; // Initialize the variable to avoid CS0165

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }

            // Recreate mixed string using input password, stored salt, and pattern
            string mixed = BooksGPT.Views.Auth.PasswordHelper.RecreateMixedString(password, user.salt, user.pattern);
            foreach (char c in user.pattern)
            {
                if (c == 'P')
                    sizeofpassword++;
            }

            // Hash the mixed string
            string hashedMixedPassword = BooksGPT.Views.Auth.PasswordHelper.GetHashPassword(mixed);

            if (!user.IsEmailVerified)
            {
                ModelState.AddModelError("", "Please verify your email before logging in.");
                return View();
            }

            if (hashedMixedPassword == user.Password && password.Count() == sizeofpassword)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, user.Name ?? ""),
                    new Claim("AvatarColor", user.AvatarColor ?? "#6b7280"),
                    new Claim(ClaimTypes.GivenName, user.Username ?? "")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                TempData["LoginSuccess"] = true;

                return View();
            }
            else
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData.Remove("LoginSuccess");
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Login");
        }
    }
}
