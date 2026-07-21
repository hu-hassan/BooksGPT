using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BooksGPT.Models;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BooksGPT.Controllers
{
    public class RegisterController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RegisterController> _logger;
        private readonly IConfiguration _configuration;

        public RegisterController(AppDbContext context, ILogger<RegisterController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        private static string GenerateRandomColor()
        {
            var rnd = new Random();
            return String.Format("#{0:X6}", rnd.Next(0x1000000));
        }

        // GET: Register
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(await _context.Users.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> GoToPassword(string email, string name)
        {
            // Check if email exists in users table
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                // Return JSON indicating email exists
                return Json(new { exists = true });
            }

            // Set the username as an HTTP-only cookie
            Response.Cookies.Append("email", email, new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true if your site uses HTTPS
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
            });
            Response.Cookies.Append("name", name ?? "", new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict
            });

            // Return JSON indicating email does not exist
            return Json(new { exists = false });
        }

        public IActionResult Password()
        {
            Request.Cookies.TryGetValue("email", out var email);
            var model = new UserModel { Email = email };


            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GoToVerification(string email, string password)
        {
            string salt = BooksGPT.Views.Auth.PasswordHelper.CreateSalt(8);

            var (mixed, pattern) = BooksGPT.Views.Auth.PasswordHelper.MixPasswordAndSalt(password, salt);

            string hashedMixedPassword = BooksGPT.Views.Auth.PasswordHelper.GetHashPassword(mixed);

            string username = email?.Split('@')[0];

            // read name from cookie if available
            Request.Cookies.TryGetValue("name", out var name);

            var user = new UserModel
            {
                Email = email,
                Username = username,
                Password = hashedMixedPassword, // Store the hashed mixed password
                salt = salt,
                pattern = pattern,
                Name = name ?? username,
                AvatarColor = GenerateRandomColor(),
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return View("VerifyEmail"); // Adjust as needed
            //return View();
        }

        public async Task<IActionResult> VerifyEmail()
        {
            // This action can be used to display a verification message or form
            Request.Cookies.TryGetValue("email", out var email);
            string verificationCode = GenerateVerificationCode();
            SendVerificationEmailAsync(email, verificationCode).GetAwaiter().GetResult();
            var existing = await _context.EmailVerifications
.Where(e => e.Email == email && !e.IsExpired)
.ToListAsync();

            foreach (var e in existing)
            {
                e.IsExpired = true;
            }

            var newVerification = new EmailVerificationsModel
            {
                Email = email,
                VerificationCode = verificationCode,
                CreatedAt = DateTime.UtcNow,
                Expiresat = DateTime.UtcNow.AddMinutes(10),
                IsExpired = false
            };

            _context.EmailVerifications.Add(newVerification);
            await _context.SaveChangesAsync();


            var verification = await _context.EmailVerifications
    .Where(e => e.Email == email && !e.IsExpired && e.Expiresat > DateTime.UtcNow)
    .OrderByDescending(e => e.CreatedAt)
    .FirstOrDefaultAsync();

            string verificationCode1 = verification.VerificationCode;
            return View();
            
        }

        [HttpPost]
        public async Task<IActionResult> VerifyEmail2(string verificationCode)
        {
            Request.Cookies.TryGetValue("email", out var email);
            var verification = await _context.EmailVerifications
                .Where(e => e.Email == email && e.VerificationCode == verificationCode && !e.IsExpired && e.Expiresat > DateTime.UtcNow)
                .FirstOrDefaultAsync();
            if (verification != null)
            {
                verification.IsExpired = true;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user != null)
                {
                    user.IsEmailVerified = true;
                    await _context.SaveChangesAsync();

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
                }
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = "Invalid or expired verification code." });
            }
        }

        public static string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); // e.g., "473281"
        }

        public async Task SendVerificationEmailAsync(string email, string code)
        {
            var smtpEmail = _configuration["Smtp:Email"] ?? Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "";
            var smtpPassword = _configuration["Smtp:Password"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(smtpEmail, smtpPassword),
                EnableSsl = true,
            };

            var message = new MailMessage
            {
                From = new MailAddress(smtpEmail, "BooksGPT"),
                Subject = "Email Verification Code",
                Body = $"Your verification code is <b>{code}</b>. It will expire in 10 minutes.",
                IsBodyHtml = true
            };
            message.To.Add(email);

            await smtpClient.SendMailAsync(message);
        }

    }
}
