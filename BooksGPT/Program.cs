using BooksGPT.Controllers;
using BooksGPT.Models;
using BooksGPT.Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
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

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}


