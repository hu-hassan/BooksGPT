using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BooksGPT.Models;
using Microsoft.Extensions.Logging;
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

            // Compare with stored password
            if (hashedMixedPassword == user.Password && password.Count() == sizeofpassword)
            {
                // Authentication successful
                TempData["LoginSuccess"] = true;
                Response.Cookies.Append("isLogin", true.ToString(), new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Set to true if your site uses HTTPS
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
                });

                Response.Cookies.Append("email", email, new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // Set to true if your site uses HTTPS
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
                });

                // set name and avatar color cookies
                try
                {
                    if (!string.IsNullOrEmpty(user.Name))
                    {
                        Response.Cookies.Append("name", user.Name, new Microsoft.AspNetCore.Http.CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
                        });
                    }
                    if (!string.IsNullOrEmpty(user.AvatarColor))
                    {
                        Response.Cookies.Append("avatarColor", user.AvatarColor, new Microsoft.AspNetCore.Http.CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
                        });
                    }
                }
                catch { }

                // stay on the login page so the client-side popup can control navigation
                return View();
            }
            else
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View();
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // clear auth cookies using Delete so browser removes them
            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Path = "/"
            };

            try
            {
                Response.Cookies.Delete("isLogin", cookieOptions);
                Response.Cookies.Delete("email", cookieOptions);
                Response.Cookies.Delete("name", cookieOptions);
                Response.Cookies.Delete("avatarColor", cookieOptions);
            }
            catch { }

            // clear TempData and session to avoid leftover flags
            TempData.Remove("LoginSuccess");
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Login");
        }
    }
}
