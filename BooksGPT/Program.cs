using BooksGPT.Controllers;
using BooksGPT.Models;
using BooksGPT.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using log4net;

namespace BooksGPT;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<BooksGPT.Utitlities>();
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddSession();
        builder.Services.AddHttpContextAccessor();
        
        // Register Handlers
        builder.Services.AddScoped<BookHandler>();
        builder.Services.AddScoped<ChatHandler>();
        builder.Services.AddScoped<GeminiHandler>();
        builder.Services.AddScoped<SessionHandler>();
        builder.Services.AddScoped<ValidationHandler>();
        builder.Services.AddScoped<ChatHistoryHandler>();
        builder.Services.AddScoped<ProfileHandler>();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "BooksGPT.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                options.LoginPath = "/Login";
                options.LogoutPath = "/Login/Logout";
                options.AccessDeniedPath = "/Login";
            });

        var app = builder.Build();
        app.UseSession();
        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}


